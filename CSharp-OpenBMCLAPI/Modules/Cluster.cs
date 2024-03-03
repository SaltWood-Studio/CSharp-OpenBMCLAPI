using Avro;
using Avro.IO;
using Avro.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;
using ZstdSharp;
using Avro.Generic;
using Downloader;
using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Security.Policy;

namespace CSharpOpenBMCLAPI.Modules
{
    public class Cluster : RunnableBase
    {
        private ClusterInfo clusterInfo;
        private TokenManager token;
        private HttpClient client;
        public Guid guid;
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

            // 检查文件
            await CheckFiles();

            return returns;
        }

        protected async Task CheckFiles()
        {
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

            object[] f = new GenericDatumReader<object[]>(schema, schema).Read(null!, decoder);

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

            Parallel.ForEach(f, (obj) =>
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

        protected async Task DownloadFile(DownloadService service, string path, string hash)
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
    }
}
