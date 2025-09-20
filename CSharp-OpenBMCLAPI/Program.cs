using CSharpOpenBMCLAPI.Modules;
using Newtonsoft.Json;
using TeraIO.Runnable;
using YamlDotNet.Serialization;

namespace CSharpOpenBMCLAPI
{
    internal class Program : RunnableBase
    {
        public Program() : base() { }

        static void Main(string[] args)
        {
            Logger.Instance.LogSystem($"Starting CSharp-OpenBMCLAPI v{PublicData.Config.clusterVersion}");
            Logger.Instance.LogSystem("高性能、低メモリ占有！");
            Logger.Instance.LogSystem($"运行时环境：{Utils.GetRuntime()}");
            Program program = new Program();
            program.Start();
            program.WaitForStop();
        }

        protected Config GetConfig()
        {
            const string configFileName = "config.yml";
            string configPath = Path.Combine(PublicData.Config.clusterWorkingDirectory, configFileName);
            if (!File.Exists(configPath))
            {
                Config config = new Config();
                Serializer serializer = new Serializer();
                File.WriteAllText(configPath, serializer.Serialize(config));
                return config;
            }
            else
            {
                string file = File.ReadAllText(configPath);
                Deserializer deserializer = new Deserializer();
                Config? config = deserializer.Deserialize<Config>(file);
                Config result;
                if (config != null)
                {
                    result = config;
                }
                else
                {
                    result = new Config();
                }
                Serializer serializer = new Serializer();
                File.WriteAllText(configPath, serializer.Serialize(config));
                return result;
            }
        }

        protected override int Run(string[] args)
        {
            try
            {
                Directory.CreateDirectory(PublicData.Config.clusterWorkingDirectory);
                Directory.CreateDirectory("working");
                const string bsonFile = "totals.bson";
                string bsonFilePath = Path.Combine(PublicData.Config.clusterWorkingDirectory, bsonFile);
                PublicData.Config = GetConfig();

                int returns = 0;

                const string environment = "working/.env.json";

                if (!File.Exists(environment)) throw new FileNotFoundException($"请在程序目录下新建 {environment} 文件，然后填入 \"ClusterId\" 和 \"ClusterSecret\"以启动集群！");

                // 从 .env.json 读取密钥然后 FetchToken
                ClusterInfo info = JsonConvert.DeserializeObject<ClusterInfo>(File.ReadAllTextAsync(environment).Result);
                PublicData requiredData = new(info);
                Logger.Instance.LogSystem($"Cluster id: {info.clusterId}");
                TokenManager token = new TokenManager(info);
                token.FetchToken().Wait();

                requiredData.Token = token;

                Cluster cluster = new(requiredData);
                Logger.Instance.LogSystem($"成功创建 Cluster 实例");
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => Utils.ExitCluster(cluster).Wait();
                Console.CancelKeyPress += (sender, e) =>
                {
                    Utils.ExitCluster(cluster).Wait();
                    Environment.Exit(0);
                };

                cluster.Start();
                return returns;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ExceptionToDetail());
                Console.ReadKey();
                return -1;
            }
        }
    }
}
