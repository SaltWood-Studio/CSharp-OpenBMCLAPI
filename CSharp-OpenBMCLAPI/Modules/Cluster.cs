using Avro;
using Avro.Generic;
using Avro.IO;
using Downloader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SocketIOClient;
using System.Diagnostics;
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
    public class Cluster : RunnableBase
    {
        private ClusterInfo clusterInfo;
        private TokenManager token;
        private HttpClient client;
        public Guid guid;
        private SocketIOClient.SocketIO socket;
        public bool IsEnabled { get; private set; }
        private Task? _keepAlive;
        //List<Task> tasks = new List<Task>();

        public Cluster(ClusterInfo info, TokenManager token) : base()
        {
            this.clusterInfo = info;
            this.token = token;
            this.guid = Guid.NewGuid();

            // Fetch 一下以免出现问题
            this.token.FetchToken().Wait();

            client = new HttpClient();
            client.BaseAddress = HttpRequest.client.BaseAddress;
            client.DefaultRequestHeaders.Add("User-Agent", $"openbmclapi-cluster/{SharedData.Config.clusterVersion}");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token.token}");

            this.socket = new(HttpRequest.client.BaseAddress?.ToString(), new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Auth = new
                {
                    token = token.Token.token
                }
            });

        }

        private void HandleError(SocketIOResponse resp)
        {
            return;
        }

        protected override int Run(string[] args)
        {
            // 工作进程启动
            SharedData.Logger.LogInfo($"工作进程 {guid} 已启动");
            Task<int> task = AsyncRun();
            task.Wait();
            return task.Result;
        }

        protected async Task<int> AsyncRun()
        {
            int returns = 0;

            await GetConfiguration();
            // 检查文件
            await CheckFiles();

            Connect();

            await RequestCertification();
            //t.Start();

            InitializeService();

            await Enable();

            SharedData.Logger.LogInfo($"工作进程 {guid} 在 {SharedData.Config.HOST}:{SharedData.Config.PORT} 提供服务");

            _keepAlive = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(25 * 1000);
                    KeepAlive().Wait();
                    this.token.FetchToken().Wait();
                }
            });

            _keepAlive.Wait();

            return returns;
        }

        private void InitializeService()
        {
            var builder = WebApplication.CreateBuilder();
            X509Certificate2 cert = X509Certificate2.CreateFromPemFile($"{SharedData.Config.clusterFileDirectory}certifications/cert.pem",
                $"{SharedData.Config.clusterFileDirectory}certifications/key.pem");
            builder.WebHost.UseUrls(new[]
            {
                //$"https://{SharedData.ClusterInfo.ClusterID}.openbmclapi.933.moe:4000/",
                //$"https://localhost:4000/",
                $"https://{SharedData.ClusterInfo.ClusterID}.openbmclapi.933.moe:4000/",
                //$"https://localhost:4000/"
            });
            //builder.WebHost.UseKestrel(option => option.ConfigureHttpsDefaults(i => i.ServerCertificate = cert));
            var app = builder.Build();
            var path = $"{SharedData.Config.clusterFileDirectory}cache";
            app.UseStaticFiles();
            app.MapGet("/download/{hash}", (context) => HttpServerUtils.DownloadHash(context));
            app.Map("/measure/{size}", (context) => HttpServerUtils.Measure(context));
            var t = app.RunAsync();
        }

        public void Connect()
        {
            this.socket.ConnectAsync().Wait();

            this.socket.On("error", error => HandleError(error));
            this.socket.On("message", msg => SharedData.Logger.LogInfo(msg));
            this.socket.On("connect", (_) => SharedData.Logger.LogInfo("与主控连接成功"));
            this.socket.On("disconnect", (r) =>
            {
                SharedData.Logger.LogWarn($"与主控断开连接：{r}");
                this.IsEnabled = false;
            });
        }

        public async Task Enable()
        {
            await socket.EmitAsync("enable",
                (SocketIOResponse resp) =>
                {
                    SharedData.Logger.LogInfo(resp);
                    // Debugger.Break();
                    SharedData.Logger.LogInfo($"启用成功");
                },
                new
                {
                    host = SharedData.Config.HOST,
                    port = SharedData.Config.PORT,
                    version = SharedData.Config.clusterVersion,
                    byoc = SharedData.Config.byoc,
                    noFastEnable = SharedData.Config.noFastEnable
                });
        }

        public async Task Disable()
        {
            await socket.EmitAsync("disable",
                (SocketIOResponse resp) =>
                {
                    SharedData.Logger.LogInfo($"禁用成功");
                });
        }

        public async Task KeepAlive()
        {
            string time = DateTime.Now.ToStandardTimeString();
            // socket.Connected.Dump();
            await socket.EmitAsync("keep-alive",
                (SocketIOResponse resp) =>
                {
                    SharedData.Logger.LogInfo($"保活成功 at {time}");
                },
                new
                {
                    time = time
                });
        }

        public async Task GetConfiguration()
        {
            var resp = await this.client.GetAsync("openbmclapi/configuration");
            var content = await resp.Content.ReadAsStringAsync();
        }

        protected async Task CheckFiles()
        {
            SharedData.Logger.LogInfo("开始检查文件");
            var resp = await client.GetAsync("openbmclapi/files");
            byte[] buffer = await resp.Content.ReadAsByteArrayAsync();
            var decomporess = new Decompressor();
            Task.Run(() =>
            {
                var data = decomporess.Unwrap(buffer);
                buffer = new byte[data.Length];
                data.CopyTo(buffer);
            }).Wait();

            string avroString = @"{""type"": ""array"",""items"": {""type"": ""record"",""name"": ""fileinfo"",""fields"": [{""name"": ""path"", ""type"": ""string""},{""name"": ""hash"", ""type"": ""string""},{""name"": ""size"", ""type"": ""long""}]}}";

            Schema schema = Schema.Parse(avroString);

            Avro.IO.Decoder decoder = new BinaryDecoder(new MemoryStream(buffer));

            object[] files = new GenericDatumReader<object[]>(schema, schema).Read(null!, decoder);

            DownloadConfiguration option = new DownloadConfiguration()
            {
                ChunkCount = 10,
                ParallelDownload = true,
                RequestConfiguration =
                {
                    Headers =
                    {
                        ["Authorization"] = $"Bearer {client.DefaultRequestHeaders.Authorization?.Parameter}",
                        ["User-Agent"] = client.DefaultRequestHeaders.UserAgent.ToString()
                    }
                }
            };

            Parallel.ForEach(files, (obj) =>
            {
                GenericRecord? record = obj as GenericRecord;
                if (record != null)
                {
                    object t;
                    record.TryGetValue("path", out t);
                    string path = t.ToString().ThrowIfNull();
                    record.TryGetValue("hash", out t);
                    string hash = t.ToString().ThrowIfNull();
                    record.TryGetValue("size", out t);
                    long size;

                    if (long.TryParse(t.ToString().ThrowIfNull(), out size))
                    {
                        DownloadService service = new DownloadService(option);
                        DownloadFile(service, path, hash).Wait();
                    }
                }
            });
        }

        private async Task DownloadFile(DownloadService service, string path, string hash)
        {
            if (!File.Exists($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}"))
            {
                service.DownloadFileCompleted += (sender, e) => Service_DownloadFileCompleted(sender, e, path, hash, service);
                await service.DownloadFileTaskAsync($"{client.BaseAddress}openbmclapi/download/{hash}", $"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
            }
        }

        private void Service_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e, string path, string hash, DownloadService service)
        {
            SharedData.Logger.LogInfo($"文件 {path} 下载完毕");
            // CheckFileAfterDownload(path, hash, service);
        }

        private void CheckFileAfterDownload(string path, string hash, DownloadService service)
        {
            var file = File.ReadAllBytes($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
            string realHash = Convert.ToHexString(MD5.HashData(file)).ToLower();
            if (realHash != hash.ToLower())
            {
                SharedData.Logger.LogInfo($"文件损坏：{path}，期望 “{hash}”，但结果为{realHash}");
                service.DownloadFileTaskAsync($"{client.BaseAddress}openbmclapi/download/{hash}").Wait();
            }
        }

        public async Task RequestCertification()
        {
            await socket.EmitAsync("request-cert", (SocketIOResponse resp) =>
            {
                var data = resp;
                //Debugger.Break();
                var json = data.GetValue<JsonElement>(0)[1];
                JsonElement cert; json.TryGetProperty("cert", out cert);
                JsonElement key; json.TryGetProperty("key", out key);

                string? certString = cert.GetString();
                string? keyString = key.GetString();

                string certPath = $"{SharedData.Config.clusterFileDirectory}certifications/cert.pem";
                string keyPath = $"{SharedData.Config.clusterFileDirectory}certifications/key.pem";

                Directory.CreateDirectory($"{SharedData.Config.clusterFileDirectory}certifications");

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
