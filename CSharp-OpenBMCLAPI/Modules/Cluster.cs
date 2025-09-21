using CSharpOpenBMCLAPI.Modules.Storage;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;
using ShellProgressBar;
using SocketIOClient;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using ZstdSharp;

namespace CSharpOpenBMCLAPI.Modules
{

    /// <summary>
    /// Cluster 实例，进行最基本的节点服务
    /// </summary>
    public class Cluster
    {
        internal ClusterInfo clusterInfo;
        public readonly TokenManager token;
        private HttpClient _client;
        public Guid guid;
        private SocketIOClient.SocketIO _socket;
        public bool IsEnabled { get; set; }
        public bool WantEnable { get; set; }
        public Configuration Configuration { get; protected set; }

        internal readonly IStorage storage;
        protected AccessCounter counter;
        public readonly CancellationTokenSource cancellationSrc = new();
        internal AppContext context;
        private List<ApiFileInfo> files;
        private WebApplication? application;

        //List<Task> tasks = new List<Task>();

        /// <summary>
        /// 构造函数，实际上 <seealso cref="Exception"/> 根本不可能被抛出
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="Exception"></exception>
        public Cluster(AppContext context)
        {
            this.context = context;
            this.clusterInfo = context.ClusterInfo;
            this.token = context.Token;
            this.guid = Guid.NewGuid();

            _client = HttpRequest.client;
            _client.DefaultRequestHeaders.Authorization = new("Bearer", context.Token?.Token.token);

            switch (AppContext.Config.StorageType)
            {
                case StorageType.File:
                    this.storage = new FileStorage(AppContext.Config.clusterFileDirectory);
                    break;
                case StorageType.WebDav:
                    throw new Exception("WebDav storage is deprecated, use Alist instead.");
                case StorageType.Alist:
                    this.storage = new AlistStorage();
                    break;
                default:
                    throw new ArgumentException($"Argument out of range. {AppContext.Config.StorageType}");
            }
            if (AppContext.Config.maxCachedMemory != 0) this.storage = new CachedStorage(this.storage);
            this.files = new List<ApiFileInfo>();
            this.counter = new();
            this._socket = InitializeSocket();
        }

        /// <summary>
        /// 初始化连接到主控用的 Socket
        /// </summary>
        protected SocketIOClient.SocketIO InitializeSocket()
        {
            return new(HttpRequest.client.BaseAddress?.ToString(), new SocketIOOptions()
            {
                Auth = new
                {
                    token = this.token.Token.token
                }
            });
        }

        /// <summary>
        /// 用于处理报错信息（其实就是打印出来罢了）
        /// </summary>
        /// <param name="resp"></param>
        private void HandleError(SocketIOResponse resp) => Utils.PrintResponseMessage(resp);

        /// <summary>
        /// 重写 <seealso cref="RunnableBase.Run(string[])"/>，Cluster 实例启动（调用 <seealso cref="RunnableBase.Start()"/> 方法时调用）
        /// </summary>
        /// <returns></returns>
        public int Start()
        {
            // 工作进程启动
            Logger.Instance.LogSystem($"工作进程 {guid} 已启动");
            Task<int> task = AsyncRun();
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// 套了一层，真正运行 Cluster 时的代码部分在这里
        /// 就是加了 <seealso cref="async"/> 而已，方便运行方法
        /// </summary>
        /// <returns></returns>
        private async Task<int> AsyncRun()
        {
            int returns = 0;

            this.storage.Initialize();

            // 检查文件
            // if (!ClusterRequiredData.Config.noEnable)
            Connect();

            await GetConfiguration();

            await CheckFiles();
            Logger.Instance.LogInfo();

            await RequestCertificate();
            InitializeService();


            if (!AppContext.Config.NoEnable) await Enable();

            Logger.Instance.LogSystem($"工作进程 {guid} 在 <{AppContext.Config.HOST}:{AppContext.Config.PORT}> 提供服务");

            Tasks.CheckFile = AppContext.Config.skipCheck ? null : new Timer(state =>
            {
                for (int i = 0; i < 36; i++)
                {
                    cancellationSrc.Token.ThrowIfCancellationRequested();
                    FetchFiles(false, FileVerificationMode.SizeOnly).Wait();
                }

                cancellationSrc.Token.ThrowIfCancellationRequested();
                cancellationSrc.Token.ThrowIfCancellationRequested();
                FetchFiles(false, FileVerificationMode.Hash).Wait();
            }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            Tasks.KeepAlive = Task.Run(_KeepAlive, cancellationSrc.Token);

            Tasks.KeepAlive.Wait();

            return returns;
        }

        private async Task _KeepAlive()
        {
            while (true)
            {
                cancellationSrc.Token.ThrowIfCancellationRequested();
                await Task.Delay(15 * 1000);
                // Disable().Wait();
                await KeepAlive();
                GC.Collect();
            }
        }

        /// <summary>
        /// 加载证书、注册路由、启动 HTTPS 服务的部分
        /// </summary>
        private void InitializeService()
        {
            X509Certificate2? cert = LoadAndConvertCert();
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(AppContext.Config.PORT, cert != null ? configure =>
                {
                    configure.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    configure.UseHttps(cert);
                }
                : configure => { });
            });
            application = builder.Build();

            // 下载路由
            application.MapGet("/download/{hash}", async (HttpContext context, string hash) =>
            {
                FileAccessInfo fai = await HttpServiceProvider.DownloadHash(context, this, hash);
                this.counter.Add(fai);
            });

            // 测速路由
            application.MapGet("/measure/{size}", async (HttpContext context, int size) => await HttpServiceProvider.Measure(context, this, size));

            // 因为暂时禁用面板而注释掉

            // JS 文件提供
            // server.routes.Add(new Route
            // {
            //     MatchRegex = new Regex(@"/static/js/(.*)"),
            //     Handler = (context, cluster, match) => HttpServiceProvider.Dashboard(context, $"static/js/{match.Groups[1].Value}").Wait()
            // });

            // 面板
            // server.routes.Add(new Route
            // {
            //     MatchRegex = new Regex(@"/"),
            //     Handler = (context, cluster, match) => HttpServiceProvider.Dashboard(context).Wait()
            // });

            application.RunAsync();
        }

        /// <summary>
        /// 顾名思义，把 PEM 证书转换成 PFX 证书，不然 Kestrel 加载不报错但关闭连接不能用
        /// </summary>
        /// <returns>
        /// 转换完成的 PFX 证书
        /// </returns>
        private X509Certificate2? LoadAndConvertCert()
        {
            if (AppContext.Config.NoCertificate) return null;
            (string certPath, string keyPath) = (Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates/cert.pem"),
                Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates/key.pem"));
            if (!File.Exists(certPath) || !File.Exists(keyPath))
            {
                return null;
            }
            X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            //return cert;
            byte[] pfxCert = cert.Export(X509ContentType.Pfx);
            Logger.Instance.LogDebug($"将 PEM 格式的证书转换为 PFX 格式");
            using (var file = File.Create(Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates/cert.pfx")))
            {
                file.Write(pfxCert);
            }
            cert = new X509Certificate2(pfxCert);
            // SharedData.Logger.LogInfo($"证书信息：\n{cert.ToString()}");
            return cert;
        }

        /// <summary>
        /// 连接 Socket、注册指令处理部分
        /// </summary>
        private void Connect()
        {
            this._socket.ConnectAsync().Wait();

            this._socket.On("error", error => HandleError(error));
            this._socket.On("message", msg => Utils.PrintResponseMessage(msg));
            this._socket.On("connect", (_) => Logger.Instance.LogInfo("与主控连接成功"));
            this._socket.On("disconnect", (r) =>
            {
                Logger.Instance.LogWarn($"与主控断开连接：{r}");
                this.IsEnabled = false;
                if (this.WantEnable)
                {
                    this.Connect();
                    this.Enable().Wait();
                }
            });
            this._socket.On("warden-error", (r) =>
            {
                Logger.Instance.LogError($"收到主控的巡检错误：{r}");
            });
        }

        /// <summary>
        /// 启用节点，向主控发送 enable 包
        /// </summary>
        /// <returns></returns>
        public async Task Enable()
        {
            if (_socket.Connected && IsEnabled)
            {
                Logger.Instance.LogWarn($"试图在节点禁用且连接未断开时调用 Enable");
                return;
            }
            await _socket.EmitAsync("enable", (SocketIOResponse resp) =>
            {
                this.IsEnabled = true;
                this.WantEnable = true;
                Utils.PrintResponseMessage(resp);
                // Debugger.Break();
                Logger.Instance.LogSystem($"启用成功");
            }, new
            {
                host = AppContext.Config.HOST,
                port = AppContext.Config.PORT,
                version = AppContext.Config.clusterVersion,
                byoc = AppContext.Config.BringYourOwnCertficate,
                noFastEnable = AppContext.Config.NoFastEnable,
                flavor = new
                {
                    runtime = Utils.GetRuntime(),
                    storage = Utils.GetStorageType(this.storage)
                }
            });
        }


        /// <summary>
        /// 禁用节点，向主控发送 disable 包
        /// </summary>
        /// <returns></returns>
        public async Task Disable()
        {
            if (!this.IsEnabled)
            {
                Logger.Instance.LogWarn($"试图在节点禁用时调用 Disable");
                return;
            }
            await _socket.EmitAsync("disable", (SocketIOResponse resp) =>
            {
                Utils.PrintResponseMessage(resp);
                Logger.Instance.LogSystem($"禁用成功");
                this.IsEnabled = false;
            });
            if (this.WantEnable)
            {
                await this.Enable();
            }
        }

        /// <summary>
        /// 保活，让主控知道节点没死。一般 25 秒调用一次
        /// </summary>
        /// <returns></returns>
        public async Task KeepAlive()
        {
            if (!this.IsEnabled)
            {
                Logger.Instance.LogWarn($"试图在节点禁用时调用 KeepAlive");
                return;
            }
            string time = DateTime.Now.ToStandardTimeString();
            // socket.Connected.Dump();
            await _socket.EmitAsync("keep-alive",
                (SocketIOResponse resp) =>
                {
                    _keepAliveMessageParser(resp);
                    this.counter.Reset();
                },
                new
                {
                    time = time,
                    hits = this.counter.Hits,
                    bytes = this.counter.Bytes
                });
        }

        private void _keepAliveMessageParser(SocketIOResponse resp)
        {
            try
            {
                var returns = resp.GetValue<List<JsonElement>>(0);
                string? message = returns.First().GetString();
                bool enabled = returns.Last().ValueKind != JsonValueKind.False;
                if (enabled)
                {
                    string? time = returns.Last().GetString();
                    Logger.Instance.LogSystem($"保活成功 at {time}，served {Utils.GetLength(this.counter.Bytes)}({this.counter.Bytes} bytes)/{this.counter.Hits} hits");
                }
                else
                {
                    this.IsEnabled = false;
                    if (this.WantEnable)
                    {
                        Logger.Instance.LogError($"保活失败：{resp}，将在 10 分钟后重新上线");
                        Task.Run(() =>
                        {
                            Thread.Sleep(10 * 60 * 1000);
                            this.Enable().Wait();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"未知原因导致打印 {nameof(KeepAlive)} 信息错误：{ex.GetType().Name}");
            }
        }

        /// <summary>
        /// 获取 Configuration
        /// </summary>
        /// <returns></returns>
        private async Task GetConfiguration()
        {
            var resp = await this._client.GetAsync("openbmclapi/configuration");
            var content = await resp.Content.ReadAsStringAsync();
            this.Configuration = JsonConvert.DeserializeObject<Configuration>(content);
            Logger.Instance.LogDebug($"同步策略：{this.Configuration.Sync.Source}，线程数：{this.Configuration.Sync.Concurrency}");
            this.context.maxThreadCount = Math.Max(AppContext.Config.DownloadFileThreads, this.Configuration.Sync.Concurrency);
            this.context.SemaphoreSlim = new SemaphoreSlim(this.context.maxThreadCount);
        }

        /// <summary>
        /// 默认的检查文件行为
        /// </summary>
        /// <returns></returns>
        private async Task CheckFiles() => await CheckFiles(AppContext.Config.skipCheck, AppContext.Config.startupCheckMode);

        /// <summary>
        /// 获取文件列表、检查文件、下载文件部分
        /// </summary>
        /// <returns></returns>
        private async Task CheckFiles(bool skipCheck, FileVerificationMode mode)
        {
            if (skipCheck || mode == FileVerificationMode.None)
            {
                return;
            }
            Logger.Instance.LogDebug($"文件检查策略：{mode}");
            var updatedFiles = await GetFileList(this.files);
            if (updatedFiles != null && updatedFiles.Count != 0)
            {
                this.files = updatedFiles;
            }

            if (this.Configuration.Sync.Concurrency < context.maxThreadCount)
            {
                Logger.Instance.LogWarn($"WARNING: 同步策略的线程数小于下载文件线程数，强制覆写线程数为 {context.maxThreadCount}");
                Logger.Instance.LogWarn($"WARNING: 覆写同步线程数为开发测试功能，无必要请勿使用！");
            }

            Console.WriteLine($"总文件大小：{Utils.GetLength(this.files.Sum(f => f.size))}，总文件数：{this.files.Count}");

            object countLock = new();

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true,
                ShowEstimatedDuration = true
            };
            using var pbar = new ProgressBar(files.Count, "Check files", options);

            Parallel.ForEach(files, file =>
            //foreach (var file in files)
            {
                CheckSingleFile(file);
                lock (countLock)
                {
                    pbar.Tick($"Threads: {context.maxThreadCount - context.SemaphoreSlim.CurrentCount}/{context.maxThreadCount}, Files: {pbar.CurrentTick}/{files.Count}");
                }
            });

            countLock = null!;
        }

        /// <summary>
        /// 获取文件列表、检查文件、下载文件部分
        /// </summary>
        /// <returns></returns>
        private async Task FetchFiles(bool skipCheck, FileVerificationMode mode)
        {
            if (skipCheck || mode == FileVerificationMode.None)
            {
                return;
            }
            Logger.Instance.LogDebug($"文件检查策略：{mode}");
            var updatedFiles = await GetFileList(this.files);
            if (updatedFiles.Count != 0)
            {
                this.files = updatedFiles;
            }

            object countLock = new();

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true,
                ShowEstimatedDuration = true
            };
            var pbar = new ProgressBar(files.Count, "Fetch files", options);

            Parallel.ForEach(files, file =>
            {
                FetchFileFromCenter(file.hash).Wait();
                lock (countLock)
                {
                    pbar.Tick($"Threads: {context.maxThreadCount - context.SemaphoreSlim.CurrentCount}/{context.maxThreadCount}, Files: {pbar.CurrentTick}/{files.Count}");
                }
            });
        }

        /// <summary>
        /// 获取完整的文件列表
        /// </summary>
        /// <returns></returns>
        private async Task<List<ApiFileInfo>> GetFileList()
        {
            var resp = await this._client.GetAsync("openbmclapi/files");
            Logger.Instance.LogDebug($"检查文件结果：{resp.StatusCode}");

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
            UnpackBytes(ref bytes);

            AvroParser avro = new AvroParser(bytes);

            var list = avro.Parse();
            return list;
        }

        /// <summary>
        /// 增量更新文件列表
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private async Task<List<ApiFileInfo>> GetFileList(List<ApiFileInfo>? files)
        {
            if (files == null) return await GetFileList();

            List<ApiFileInfo> updatedFiles;

            if (files.Count == 0)
            {
                return await GetFileList();
            }
            long lastModified = files.Select(f => f.mtime).Max();

            var resp = await this._client.GetAsync($"openbmclapi/files?lastModified={lastModified}");
            Logger.Instance.LogDebug($"检查文件结果：{resp.StatusCode}");
            if (resp.StatusCode == HttpStatusCode.NotModified)
            {
                updatedFiles = new List<ApiFileInfo>();
                return updatedFiles;
            }

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
            UnpackBytes(ref bytes);

            AvroParser avro = new AvroParser(bytes);

            updatedFiles = avro.Parse();
            return updatedFiles;
        }

        /// <summary>
        /// 检查单个文件
        /// </summary>
        /// <param name="file"></param>
        void CheckSingleFile(ApiFileInfo file) => CheckSingleFile(file, AppContext.Config.startupCheckMode);

        /// <summary>
        /// 检查单个文件，并且额外指定检查模式
        /// </summary>
        /// <param name="file"></param>
        /// <param name="mode"></param>
        void CheckSingleFile(ApiFileInfo file, FileVerificationMode mode)
        {
            string path = file.path;
            string hash = file.hash;
            long size = file.size;
            bool valid = VerifyFile(hash, size, mode);
            if (!valid)
            {
                DownloadFile(hash, path, true).Wait();
            }
        }

        /// <summary>
        /// 解压从主控下发的文件列表
        /// </summary>
        /// <param name="bytes"></param>
        private static void UnpackBytes(ref byte[] bytes)
        {
            Decompressor decompressor = new Decompressor();
            bytes = decompressor.Unwrap(bytes).ToArray();
            decompressor.Dispose();
        }

        /// <summary>
        /// 验证文件，有多种验证方式，取决于枚举值 <seealso cref="FileVerificationMode"/>
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="size"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private bool VerifyFile(string hash, long size, FileVerificationMode mode)
        {
            string path = Utils.HashToFileName(hash);

            switch (mode)
            {
                case FileVerificationMode.None:
                    return true;
                case FileVerificationMode.Exists:
                    return this.storage.Exists(path);
                case FileVerificationMode.SizeOnly:
                    if (!VerifyFile(hash, size, FileVerificationMode.Exists)) return false;
                    return size == this.storage.GetFileSize(path);
                case FileVerificationMode.Hash:
                    if (!VerifyFile(hash, size, FileVerificationMode.SizeOnly)) return false;
                    var file = this.storage.ReadFileStream(path);
                    return Utils.ValidateFile(file, hash);
                default:
                    return true;
            }
        }

        /// <summary>
        /// 根据哈希值下载文件
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        private async Task FetchFileFromCenter(string hash, bool force = false)
        {
            string filePath = Utils.HashToFileName(hash);
            if (this.storage.Exists(filePath) && !force)
            {
                return;
            }

            this.context.SemaphoreSlim.Wait();
            try
            {
                var resp = await this._client.GetAsync($"openbmclapi/download/{hash}");
                this.storage.WriteFileStream(Utils.HashToFileName(hash), await resp.Content.ReadAsStreamAsync());
            }
            finally
            {
                this.context.SemaphoreSlim.Release();
            }
        }

        private (HttpResponseMessage?, List<string>, Exception?) GetRedirectUrls(string url)
        {
            var redirectUrls = new List<string>();
            UriBuilder builder = new UriBuilder(this._client.BaseAddress?.ToString() ?? string.Empty);
            builder.Path = url;
            redirectUrls.Add(builder.ToString());
            HttpResponseMessage? response = null;
            HttpClient requestClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            {
                BaseAddress = HttpRequest.client.BaseAddress
            };

            try
            {
                string currentUrl = url;
                while (true)
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, currentUrl);

                    response = requestClient.Send(request);

                    // 检查响应状态码
                    if (response.StatusCode < HttpStatusCode.BadRequest && response.StatusCode > HttpStatusCode.OK)
                    {
                        // 获取重定向的URL
                        response.Headers.TryGetValues("Location", out IEnumerable<string>? _values);
                        string? redirectUrl = _values?.FirstOrDefault();
                        if (string.IsNullOrEmpty(redirectUrl))
                        {
                            break;
                        }

                        redirectUrls.Add(redirectUrl);
                        currentUrl = redirectUrl; // 更新URL以继续跟踪重定向
                    }
                    else
                    {
                        // 如果不是3xx状态码，返回最终的响应
                        break;
                    }
                }
                return (response, redirectUrls, null);
            }
            catch (Exception ex)
            {
                return (response, redirectUrls, ex);
            }
        }

        /// <summary>
        /// 根据哈希值从主控拉取文件
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        private async Task DownloadFile(string hash, string path, bool force = false)
        {
            string filePath = Utils.HashToFileName(hash);
            if (this.storage.Exists(filePath) && !force)
            {
                return;
            }

            await this.context.SemaphoreSlim.WaitAsync();
            List<string> urls = new List<string>();
            try
            {
                HttpResponseMessage? resp = null;
                (resp, urls, var exception) = GetRedirectUrls(path[1..]);
                if (exception != null) throw new AggregateException(exception);
                if (resp == null) throw new Exception("Response is null.");
                if (resp.StatusCode < HttpStatusCode.BadRequest && resp.StatusCode >= HttpStatusCode.OK)
                {
                    this.storage.WriteFileStream(Utils.HashToFileName(hash), await resp.Content.ReadAsStreamAsync());
                }
                else
                {
                    throw new Exception($"Response code {resp.StatusCode}, 2xx or 3xx expected.");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await this._client.PostAsJsonAsync("openbmclapi/report", new
                    {
                        urls = urls,
                        error = JsonConvert.SerializeObject(new
                        {
                            message = ex.Message,
                            type = ex.GetType().FullName,
                            fullMessage = ex.ExceptionToDetail(),
                            stacktrace = ex.StackTrace
                        })
                    });
                }
                catch { /* ignored */ }
            }
            finally
            {
                this.context.SemaphoreSlim.Release();
            }
        }

        /// <summary>
        /// 请求证书
        /// </summary>
        /// <returns></returns>
        private async Task RequestCertificate()
        {
            // File.Delete(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certificates/cert.pem"));
            // File.Delete(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certificates/key.pem"));
            if (AppContext.Config.BringYourOwnCertficate)
            {
                Logger.Instance.LogDebug($"{nameof(AppContext.Config.BringYourOwnCertficate)} 为 true，跳过请求证书……");
                return;
            }
            string certPath = Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates/cert.pem");
            string keyPath = Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates/key.pem");

            TaskCompletionSource tcs = new TaskCompletionSource();

            Directory.CreateDirectory(Path.Combine(AppContext.Config.clusterWorkingDirectory, $"certificates"));
            await _socket.EmitAsync("request-cert", (SocketIOResponse resp) =>
            {
                try
                {
                    var data = resp;
                    var json = data.GetValue<JsonElement>(0)[1];
                    JsonElement cert; json.TryGetProperty("cert", out cert);
                    JsonElement key; json.TryGetProperty("key", out key);

                    string? certString = cert.GetString();
                    string? keyString = key.GetString();

                    using (var file = File.Create(certPath))
                    {
                        if (certString != null) file.Write(Encoding.UTF8.GetBytes(certString));
                    }

                    using (var file = File.Create(keyPath))
                    {
                        if (keyString != null) file.Write(Encoding.UTF8.GetBytes(keyString));
                    }

                    Logger.Instance.LogDebug($"获取证书成功！");
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }
    }
}
