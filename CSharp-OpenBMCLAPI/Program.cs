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
            TokenManager manager = new TokenManager(info);
            ExtensionMethods.PrintTypeInfo(await manager.FetchToken());

            return returns;
        }
    }
}
