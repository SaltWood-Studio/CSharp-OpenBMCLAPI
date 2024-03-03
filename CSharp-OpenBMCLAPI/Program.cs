using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CSharpOpenBMCLAPI.Modules;
using Newtonsoft.Json;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI
{
    internal class Program : RunnableBase
    {
        public Program() : base() { }

        static void Main(string[] args)
        {
            SharedData.Logger.LogInfo($"Starting CSharp-OpenBMCLAPI v{SharedData.Config.clusterVersion}");
            Program program = new Program();
            program.Start();
            program.WaitForStop();
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

            // 从 .env.json 读取密钥然后 FetchToken
            ClusterInfo info = JsonConvert.DeserializeObject<ClusterInfo>(await File.ReadAllTextAsync(".env.json"));
            SharedData.Logger.LogInfo($"Cluster id: {info.ClusterID}");
            TokenManager token = new TokenManager(info);

            SharedData.Logger.LogInfo($"成功创建 Cluster 实例");
            Cluster cluster = new(info, token);
            cluster.Start();
            cluster.WaitForStop();

            return returns;
        }
    }
}
