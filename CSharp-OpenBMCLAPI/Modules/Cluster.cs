using CSharpOpenBMCLAPI.Modules.Plugin;
using CSharpOpenBMCLAPI.Modules.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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
using TeraIO.Network.Http;
using TeraIO.Runnable;
using ZstdSharp;

namespace CSharpOpenBMCLAPI.Modules
{

    /// <summary>
    /// Cluster 实例，进行最基本的节点服务
    /// </summary>
    public class Cluster : RunnableBase
    {
        private ClusterInfo clusterInfo;
        private TokenManager token;
        private HttpClient client;
        public Guid guid;
        private SocketIOClient.SocketIO socket;
        public bool IsEnabled { get; set; }
        public bool WantEnable { get; set; }
        private Task? _keepAlive;
        internal IStorage storage;
        protected AccessCounter counter;
        public CancellationTokenSource cancellationSrc = new CancellationTokenSource();
        public WebApplication? application = null;
        //List<Task> tasks = new List<Task>();

        /// <summary>
        /// 构造函数，实际上 <seealso cref="Exception"/> 根本不可能被抛出
        /// </summary>
        /// <param name="info"></param>
        /// <param name="token"></param>
        /// <exception cref="Exception"></exception>
        public Cluster(ClusterInfo info, TokenManager token) : base()
        {
            this.clusterInfo = info;
            this.token = token;
            this.guid = Guid.NewGuid();

            client = HttpRequest.client;
            client.DefaultRequestHeaders.Authorization = new("Bearer", SharedData.Token?.Token.token);

            this.storage = new CachedStorage(new FileStorage(SharedData.Config.clusterFileDirectory));

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
        protected override int Run(string[] args)
        {
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ClusterStarted);
            // 工作进程启动
            SharedData.Logger.LogSystem($"工作进程 {guid} 已启动");
            Task<int> task = AsyncRun();
            task.Wait();
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ClusterStopped);
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

            // await GetConfiguration();
            // 检查文件
            await CheckFiles();
            SharedData.Logger.LogInfo();
            Connect();

            await RequestCertification();

            InitializeService();

            await Enable();

            SharedData.Logger.LogSystem($"工作进程 {guid} 在 <{SharedData.Config.HOST}:{SharedData.Config.PORT}> 提供服务");

            _keepAlive = Task.Run(_KeepAlive, cancellationSrc.Token);

            _keepAlive.Wait();

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
            var builder = WebApplication.CreateBuilder();
            X509Certificate2 cert = LoadAndConvertCert();
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
            //application.
            Task task = application.RunAsync();
            Task.Run(async () =>
            {
                Thread.Sleep(1000);
                if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
                {
                    await this.Disable();
                }
            });
        }

        /// <summary>
        /// 顾名思义，把 PEM 证书转换成 PFX 证书，不然 Kestrel 加载不报错但关闭连接不能用
        /// </summary>
        /// <returns>
        /// 转换完成的 PFX 证书
        /// </returns>
        protected X509Certificate2 LoadAndConvertCert()
        {
            X509Certificate2 cert = X509Certificate2.CreateFromPemFile(Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications/cert.pem"),
                Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications/key.pem"));
            byte[] pfxCert = cert.Export(X509ContentType.Pfx);
            SharedData.Logger.LogDebug($"将 PEM 格式的证书转换为 PFX 格式");
            using (var file = File.Create(Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications/cert.pfx")))
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
            this.socket.On("connect", (_) => SharedData.Logger.LogInfo("与主控连接成功"));
            this.socket.On("disconnect", (r) =>
            {
                SharedData.Logger.LogWarn($"与主控断开连接：{r}");
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
                SharedData.Logger.LogWarn($"试图在节点禁用且连接未断开时调用 Enable");
                return;
            }
            await socket.EmitAsync("enable", (SocketIOResponse resp) =>
            {
                this.IsEnabled = true;
                this.WantEnable = true;
                Utils.PrintResponseMessage(resp);
                // Debugger.Break();
                SharedData.Logger.LogSystem($"启用成功");
            }, new
            {
                host = SharedData.Config.HOST,
                port = SharedData.Config.PORT,
                version = SharedData.Config.clusterVersion,
                byoc = SharedData.Config.bringYourOwnCertficate,
                noFastEnable = SharedData.Config.noFastEnable,
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
                SharedData.Logger.LogWarn($"试图在节点禁用时调用 Disable");
                return;
            }
            await socket.EmitAsync("disable", (SocketIOResponse resp) =>
            {
                Utils.PrintResponseMessage(resp);
                SharedData.Logger.LogSystem($"禁用成功");
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
                SharedData.Logger.LogWarn($"试图在节点禁用时调用 KeepAlive");
                return;
            }
            string time = DateTime.Now.ToStandardTimeString();
            // socket.Connected.Dump();
            await socket.EmitAsync("keep-alive",
                (SocketIOResponse resp) =>
                {
                    Utils.PrintResponseMessage(resp);
                    SharedData.Logger.LogSystem($"保活成功 at {time}，served {Utils.GetLength(this.counter.bytes)}({this.counter.bytes} bytes)/{this.counter.hits} hits");
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
                lock (SharedData.DataStatistician)
                {
                    file.Write(Utils.BsonSerializeObject(SharedData.DataStatistician));
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
            if (SharedData.Config.skipStartupCheck || SharedData.Config.startupCheckMode == FileVerificationMode.None)
            {
                return;
            }
            var resp = await this.client.GetAsync("openbmclapi/files");
            SharedData.Logger.LogDebug($"文件检查策略：{SharedData.Config.startupCheckMode}");
            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();

            UnpackBytes(ref bytes);

            AvroParser avro = new AvroParser(bytes);
            List<ApiFileInfo> files;

            files = avro.Parse();

            object countLock = new();
            int count = 0;

            CancellationTokenSource t = new();

            _ = Task.Run(() =>
            {
                while (true)
                {
                    GC.Collect();
                    Thread.Sleep(1000);
                    t.Token.ThrowIfCancellationRequested();
                }
            }, t.Token);

            //Parallel.ForEach(files, file =>
            foreach (var file in files)
            {
                CheckSingleFile(file);
                lock (countLock)
                {
                    count++;
                }
                SharedData.Logger.LogInfoNoNewLine($"\r{count}/{files.Count}");
            }//);

            t.Cancel();

            files = null!;
            countLock = null!;
            bytes = null!;
        }

        void CheckSingleFile(ApiFileInfo file)
        {
            string path = file.path;
            string hash = file.hash;
            long size = file.size;
            DownloadFile(hash, path).Wait();
            bool valid = VerifyFile(hash, size, SharedData.Config.startupCheckMode);
            if (!valid)
            {
                SharedData.Logger.LogWarn($"文件 {path} 损坏！期望哈希值为 {hash}");
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
            SharedData.Logger.LogDebug($"文件 {path} 下载成功");
        }

        /// <summary>
        /// 请求证书
        /// </summary>
        /// <returns></returns>
        public async Task RequestCertification()
        {
            if (SharedData.Config.bringYourOwnCertficate)
            {
                SharedData.Logger.LogDebug($"{nameof(SharedData.Config.bringYourOwnCertficate)} 为 true，跳过请求证书……");
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

                string certPath = Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications/cert.pem");
                string keyPath = Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications/key.pem");

                Directory.CreateDirectory(Path.Combine(SharedData.Config.clusterFileDirectory, $"certifications"));

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
