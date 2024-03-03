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
using TeraIO.Extension;

namespace CSharpOpenBMCLAPI.Modules
{
    public class Cluster : RunnableBase
    {
        private ClusterInfo clusterInfo;
        private TokenManager token;
        private HttpClient client;

        public Cluster(ClusterInfo info, TokenManager token) : base()
        {
            this.clusterInfo = info;
            this.token = token;
            client = new HttpClient();
            client.BaseAddress = HttpRequest.client.BaseAddress;
            client.DefaultRequestHeaders.Add("User-Agent", "openbmclapi-cluster/1.9.7");
        }

        protected override int Run(string[] args)
        {
            Task<int> task = AsyncRun();
            task.Wait();
            return task.Result;
        }

        protected async Task<int> AsyncRun()
        {
            int returns = 0;

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.token.Token.token}");
            var resp = await client.GetAsync("openbmclapi/files");
            byte[] buffer = await resp.Content.ReadAsByteArrayAsync();
            var decomporess = new Decompressor();
            Task.Run(() =>
            {
                var data = decomporess.Unwrap(buffer);
                buffer = new byte[data.Length];
                data.CopyTo(buffer);
            }).Wait();
            StreamWriter sw = new("a.json");
            sw.BaseStream.Write(buffer);

            string avroString = @"{""type"": ""array"",""items"": {""type"": ""record"",""name"": ""fileinfo"",""fields"": [{""name"": ""path"", ""type"": ""string""},{""name"": ""hash"", ""type"": ""string""},{""name"": ""size"", ""type"": ""long""}]}}";

            Schema schema = Schema.Parse(avroString);

            Avro.IO.Decoder decoder = new BinaryDecoder(new MemoryStream(buffer));

            object[] f = new GenericDatumReader<object[]>(schema, schema).Read(null!, decoder);

            foreach (var obj in f )
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

                    }
                }
            }

            return returns;
        }
    }

    class FileInfo
    {
        public string path = "";
        public string hash = "";
        public long size = 0;
    }
}
