using CSharpOpenBMCLAPI.Modules.Plugin;
using CSharpOpenBMCLAPI.Modules.Storage;
using CSharpOpenBMCLAPI.Modules.WebServer;
using SocketIOClient;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TeraIO.Network.Http;
using TeraIO.Runnable;
using ZstdSharp;

namespace CSharpOpenBMCLAPI.Modules
{

    /// <summary>
    /// Cluster 实例，进行最基本的节点服务
    /// </summary>
    public class Cluster
    {
        internal ClusterInfo clusterInfo;
        private TokenManager token;
        private HttpClient client;
        public Guid guid;
        private SocketIOClient.SocketIO socket;
        public bool IsEnabled { get; set; }
        public bool WantEnable { get; set; }
        internal IStorage storage;
        protected AccessCounter counter;
        public CancellationTokenSource cancellationSrc = new CancellationTokenSource();
        internal ClusterRequiredData requiredData;
        internal List<ApiFileInfo> files;

        //List<Task> tasks = new List<Task>();

        /// <summary>
        /// 构造函数，实际上 <seealso cref="Exception"/> 根本不可能被抛出
        /// </summary>
        /// <param name="info"></param>
        /// <param name="token"></param>
        /// <exception cref="Exception"></exception>
        public Cluster(ClusterRequiredData requiredData) : base()
        {
            this.requiredData = requiredData;
            this.clusterInfo = requiredData.ClusterInfo;
            this.token = requiredData.Token;
            this.guid = Guid.NewGuid();

            client = HttpRequest.client;
            client.DefaultRequestHeaders.Authorization = new("Bearer", requiredData.Token?.Token.token);

            this.storage = new CachedStorage(new FileStorage(ClusterRequiredData.Config.clusterFileDirectory));
            this.files = new List<ApiFileInfo>();
            this.counter = new();
            InitializeSocket();

            // 用来规避构造函数退出时巴拉巴拉的提示
            if (this.socket == null)
            {
                throw new Exception("Impossible! \"socket\" field is still null.");
            }
        }

        /// <summary>
        /// 初始化连接到主控用的 Socket
        /// </summary>
        protected void InitializeSocket()
        {
            this.socket = new(HttpRequest.client.BaseAddress?.ToString(), new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
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
        public void HandleError(SocketIOResponse resp) => Utils.PrintResponseMessage(resp);

        /// <summary>
        /// 重写 <seealso cref="RunnableBase.Run(string[])"/>，Cluster 实例启动（调用 <seealso cref="RunnableBase.Start()"/> 方法时调用）
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Start()
        {
            requiredData.PluginManager.TriggerEvent(this, ProgramEventType.ClusterStarted);
            // 工作进程启动
            Logger.Instance.LogSystem($"工作进程 {guid} 已启动");
            Task<int> task = AsyncRun();
            task.Wait();
            requiredData.PluginManager.TriggerEvent(this, ProgramEventType.ClusterStopped);
            return task.Result;
        }

        /// <summary>
        /// 套了一层，真正运行 Cluster 时的代码部分在这里
        /// 就是加了 <seealso cref="async"/> 而已，方便运行方法
        /// </summary>
        /// <returns></returns>
        protected async Task<int> AsyncRun()
        {
            int returns = 0;

            // 检查文件
            if (!ClusterRequiredData.Config.noEnable) await CheckFiles();
            Logger.Instance.LogInfo();
            if (!ClusterRequiredData.Config.noEnable) Connect();

            if (!ClusterRequiredData.Config.noEnable) await GetConfiguration();

            if (!ClusterRequiredData.Config.noEnable) await RequestCertification();

            if (!ClusterRequiredData.Config.noEnable) LoadAndConvertCert();

            Logger.Instance.LogInfo($"{nameof(AsyncRun)} 正在等待证书请求……");

            while (!ClusterRequiredData.Config.noEnable)
            {
                if (File.Exists(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/key.pem")) &&
                    File.Exists(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/cert.pem")))
                {
                    break;
                }
                Thread.Sleep(200);
            }

            InitializeService();

            if (!ClusterRequiredData.Config.noEnable) await Enable();

            Logger.Instance.LogSystem($"工作进程 {guid} 在 <{ClusterRequiredData.Config.HOST}:{ClusterRequiredData.Config.PORT}> 提供服务");

            Tasks.CheckFile = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(10 * 60 * 1000);
                    CheckFiles().Wait();
                }
            });

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
            //var builder = WebApplication.CreateBuilder();
            X509Certificate2 cert = LoadAndConvertCert();
            SimpleWebServer server = new(ClusterRequiredData.Config.PORT, cert, this);//cert);

            // 下载路由
            server.routes.Add(new Route { MatchRegex = new Regex(@"/download/[0-9a-fA-F]{32,40}?.*"), ConditionExpressions =
                {
                    (path) => path.Contains("s=") && path.Contains("e=")
                },
                Handler = (context, cluster, Match) =>
                {
                    FileAccessInfo fai = HttpServiceProvider.DownloadHash(context, cluster).Result;
                    this.counter.Add(fai);
                }
            });

            // 测速路由
            server.routes.Add(new Route { MatchRegex = new Regex(@"/measure/\d"),
                Handler = (context, cluster, match) => HttpServiceProvider.Measure(context, cluster).Wait()
            });

            // API 数据
            server.routes.Add(new Route { MatchRegex = new Regex(@"/api/(.*)"),
                Handler = (context, cluster, match) => HttpServiceProvider.Api(context, match.Groups[1].Value, this).Wait(),
                Methods = "POST"
            });

            // JS 文件提供
            server.routes.Add(new Route
            {
                MatchRegex = new Regex(@"/static/js/(.*)"),
                Handler = (context, cluster, match) => HttpServiceProvider.Dashboard(context, $"static/js/{match.Groups[1].Value}").Wait()
            });

            // 面板
            server.routes.Add(new Route
            {
                MatchRegex = new Regex(@"/"),
                Handler = (context, cluster, match) => HttpServiceProvider.Dashboard(context).Wait()
            });

            server.Start();
            /*
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(SharedData.Config.PORT, listenOptions =>
                {
                    listenOptions.UseHttps(cert);
                });
            });
            application = builder.Build();
            application.UseHttpsRedirection();
            var path = Path.Combine(SharedData.Config.clusterFileDirectory, $"cache");
            // application.UseStaticFiles();
            application.MapGet("/download/{hash}", (context) => HttpServiceProvider.LogAndRun(context, () =>
            {
                FileAccessInfo fai = HttpServiceProvider.DownloadHash(context, this.storage).Result;
                this.counter.Add(fai);
            }));
            application.MapGet("/measure/{size}", (context) => HttpServiceProvider.LogAndRun(context,
                () => HttpServiceProvider.Measure(context).Wait()
            ));
            application.MapPost("/api/{name}", (HttpContext context, string name) => HttpServiceProvider.Api(context, name, this).Wait());
            application.MapGet("/", (context) =>
            {
                context.Response.StatusCode = 302;
                context.Response.Headers.Append("Location", "/dashboard");
                return Task.CompletedTask;
            });
            application.MapGet("/dashboard", (context) =>
            {
                HttpServiceProvider.Dashboard(context);
                return Task.CompletedTask;
            });
            application.MapGet("/static/js/{file}", (HttpContext context, string file) => HttpServiceProvider.Dashboard(context, $"/static/js/{file}"));

            Task task = application.RunAsync();
            Task.Run(async () =>
            {
                Thread.Sleep(1000);
                if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
                {
                    await this.Disable();
                }
            });*/
        }

        /// <summary>
        /// 顾名思义，把 PEM 证书转换成 PFX 证书，不然 Kestrel 加载不报错但关闭连接不能用
        /// </summary>
        /// <returns>
        /// 转换完成的 PFX 证书
        /// </returns>
        protected X509Certificate2 LoadAndConvertCert()
        {
            X509Certificate2 cert = X509Certificate2.CreateFromPemFile(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/cert.pem"),
                Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/key.pem"));
            //return cert;
            byte[] pfxCert = cert.Export(X509ContentType.Pfx);
            Logger.Instance.LogDebug($"将 PEM 格式的证书转换为 PFX 格式");
            using (var file = File.Create(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/cert.pfx")))
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
        public void Connect()
        {
            this.socket.ConnectAsync().Wait();

            this.socket.On("error", error => HandleError(error));
            this.socket.On("message", msg => Utils.PrintResponseMessage(msg));
            this.socket.On("connect", (_) => Logger.Instance.LogInfo("与主控连接成功"));
            this.socket.On("disconnect", (r) =>
            {
                Logger.Instance.LogWarn($"与主控断开连接：{r}");
                this.IsEnabled = false;
                if (this.WantEnable)
                {
                    this.Connect();
                    this.Enable().Wait();
                }
            });
        }

        /// <summary>
        /// 启用节点，向主控发送 enable 包
        /// </summary>
        /// <returns></returns>
        public async Task Enable()
        {
            if (socket.Connected && IsEnabled)
            {
                Logger.Instance.LogWarn($"试图在节点禁用且连接未断开时调用 Enable");
                return;
            }
            await socket.EmitAsync("enable", (SocketIOResponse resp) =>
            {
                this.IsEnabled = true;
                this.WantEnable = true;
                Utils.PrintResponseMessage(resp);
                // Debugger.Break();
                Logger.Instance.LogSystem($"启用成功");
            }, new
            {
                host = ClusterRequiredData.Config.HOST,
                port = ClusterRequiredData.Config.PORT,
                version = ClusterRequiredData.Config.clusterVersion,
                byoc = ClusterRequiredData.Config.bringYourOwnCertficate,
                noFastEnable = ClusterRequiredData.Config.noFastEnable,
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
            await socket.EmitAsync("disable", (SocketIOResponse resp) =>
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
            await socket.EmitAsync("keep-alive",
                (SocketIOResponse resp) =>
                {
                    Utils.PrintResponseMessage(resp);
                    Logger.Instance.LogSystem($"保活成功 at {time}，served {Utils.GetLength(this.counter.bytes)}({this.counter.bytes} bytes)/{this.counter.hits} hits");
                    this.counter.Reset();
                },
                new
                {
                    time = time,
                    hits = this.counter.hits,
                    bytes = this.counter.bytes
                });

            using (var file = File.Create("totals.bson"))
            {
                lock (ClusterRequiredData.DataStatistician)
                {
                    file.Write(Utils.BsonSerializeObject(ClusterRequiredData.DataStatistician));
                }
            }
        }

        /// <summary>
        /// 获取 Configuration，对于 C# 版本的节点端没啥用
        /// </summary>
        /// <returns></returns>
        public async Task GetConfiguration()
        {
            var resp = await this.client.GetAsync("openbmclapi/configuration");
            var content = await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 获取文件列表、检查文件、下载文件部分
        /// </summary>
        /// <returns></returns>
        protected async Task CheckFiles()
        {
            if (ClusterRequiredData.Config.skipStartupCheck || ClusterRequiredData.Config.startupCheckMode == FileVerificationMode.None)
            {
                return;
            }
            Logger.Instance.LogDebug($"文件检查策略：{ClusterRequiredData.Config.startupCheckMode}");
            var updatedFiles = await GetFileList(this.files);
            if (updatedFiles != null && updatedFiles.Count != 0)
            {
                this.files = updatedFiles;
            }

            object countLock = new();
            int count = 0;

            Parallel.ForEach(files, file =>
            //foreach (var file in files)
            {
                CheckSingleFile(file);
                lock (countLock)
                {
                    count++;
                }
                Logger.Instance.LogInfoNoNewLine($"\r{count}/{files.Count}");
            });

            files = null!;
            countLock = null!;
        }

        /// <summary>
        /// 获取完整的文件列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<ApiFileInfo>> GetFileList()
        {
            var resp = await this.client.GetAsync("openbmclapi/files");
            Logger.Instance.LogDebug($"检查文件结果：{resp}");

            List<ApiFileInfo> files;

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
            UnpackBytes(ref bytes);

            AvroParser avro = new AvroParser(bytes);

            files = avro.Parse();
            return files;
        }

        public async Task<List<ApiFileInfo>> GetFileList(List<ApiFileInfo>? files)
        {
            if (files == null) return await GetFileList();

            List<ApiFileInfo> updatedFiles;

            if ((files == null) || (files.Count == 0))
            {
                return await GetFileList();
            }
            long lastModified = files.Select(f => f.mtime).Max();

            var resp = await this.client.GetAsync($"openbmclapi/files?lastModified={lastModified}");
            Logger.Instance.LogDebug($"检查文件结果：{resp}");
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

        void CheckSingleFile(ApiFileInfo file)
        {
            string path = file.path;
            string hash = file.hash;
            long size = file.size;
            DownloadFile(hash, path).Wait();
            bool valid = VerifyFile(hash, size, ClusterRequiredData.Config.startupCheckMode);
            if (!valid)
            {
                Logger.Instance.LogWarn($"文件 {path} 损坏！期望哈希值为 {hash}");
                DownloadFile(hash, path, true).Wait();
            }
        }

        protected void UnpackBytes(ref byte[] bytes)
        {
            Decompressor decompressor = new Decompressor();
            bytes = decompressor.Unwrap(bytes).ToArray();
            decompressor.Dispose();
            decompressor = null!;
        }

        /// <summary>
        /// 验证文件，有多种验证方式，取决于枚举值 <seealso cref="FileVerificationMode"/>
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="size"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        protected bool VerifyFile(string hash, long size, FileVerificationMode mode)
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
                    var file = this.storage.ReadFile(path);
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
        private async Task DownloadFile(string hash, string path, bool force = false)
        {
            string filePath = Utils.HashToFileName(hash);
            if (this.storage.Exists(filePath) && !force)
            {
                return;
            }

            var resp = await this.client.GetAsync($"openbmclapi/download/{hash}");

            this.storage.WriteFile(Utils.HashToFileName(hash), await resp.Content.ReadAsByteArrayAsync());
            Logger.Instance.LogDebug($"文件 {path} 下载成功");
        }

        /// <summary>
        /// 请求证书
        /// </summary>
        /// <returns></returns>
        public async Task RequestCertification()
        {
            // File.Delete(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/cert.pem"));
            // File.Delete(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/key.pem"));
            if (ClusterRequiredData.Config.bringYourOwnCertficate)
            {
                Logger.Instance.LogDebug($"{nameof(ClusterRequiredData.Config.bringYourOwnCertficate)} 为 true，跳过请求证书……");
                return;
            }
            await socket.EmitAsync("request-cert", (SocketIOResponse resp) =>
            {
                var data = resp;
                //Debugger.Break();
                var json = data.GetValue<JsonElement>(0)[1];
                JsonElement cert; json.TryGetProperty("cert", out cert);
                JsonElement key; json.TryGetProperty("key", out key);

                string? certString = cert.GetString();
                string? keyString = key.GetString();

                string certPath = Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/cert.pem");
                string keyPath = Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications/key.pem");

                Directory.CreateDirectory(Path.Combine(ClusterRequiredData.Config.clusterFileDirectory, $"certifications"));

                using (var file = File.Create(certPath))
                {
                    if (certString != null) file.Write(Encoding.UTF8.GetBytes(certString));
                }

                using (var file = File.Create(keyPath))
                {
                    if (keyString != null) file.Write(Encoding.UTF8.GetBytes(keyString));
                }
            });
        }
    }
}
