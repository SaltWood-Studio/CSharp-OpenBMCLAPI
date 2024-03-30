using CSharpOpenBMCLAPI.Modules;
using CSharpOpenBMCLAPI.Modules.Plugin;
using CSharpOpenBMCLAPI.Modules.Statistician;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI
{
    internal class Program : RunnableBase
    {
        public Program() : base() { }

        static void Main(string[] args)
        {
            SharedData.Logger.LogSystem($"Starting CSharp-OpenBMCLAPI v{SharedData.Config.clusterVersion}");
            SharedData.Logger.LogSystem("高性能、低メモリ占有！");
            SharedData.Logger.LogSystem($"运行时环境：{Utils.GetRuntime()}");
            Program program = new Program();
            program.Start();
            program.WaitForStop();
        }

        protected void LoadPlugins()
        {
            string path = Path.Combine(SharedData.Config.clusterFileDirectory, "plugins");

            Directory.CreateDirectory(path);

            foreach (var file in Directory.GetFiles(path))
            {
                if (!file.EndsWith(".dll")) continue;
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        Type? parent = type;
                        while (parent != null)
                        {
                            if (parent == typeof(PluginBase))
                            {
                                PluginAttribute? attr = type.GetCustomAttribute<PluginAttribute>();
                                if (attr == null || !attr.Hidden)
                                {
                                    SharedData.PluginManager.RegisterPlugin(type);
                                }
                                break;
                            }
                            parent = parent.BaseType;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SharedData.Logger.LogError($"跳过加载插件 {Path.Combine(file)}。加载插件时出现未知错误。\n", Utils.ExceptionToDetail(ex));
                }
            }
        }

        protected Config GetConfig()
        {
            if (!File.Exists("config.json5"))
            {
                // 获取正在运行方法所在的命名空间空间
                Type? type = MethodBase.GetCurrentMethod()?.DeclaringType;

                string? _namespace = type?.Namespace;

                // 获取当前运行的 Assembly
                Assembly _assembly = Assembly.GetExecutingAssembly();

                // 获取资源名称
                string resourceName = $"{_namespace}.DefaultConfig.json5";

                // 从 Assembly 中提取资源
                Stream? stream = _assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    using (var file = File.Create("config.json5"))
                    {
                        file.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(file);
                    }
                }

                return new Config();
            }
            else
            {
                string file = File.ReadAllText("config.json5");
                Config? config = JsonConvert.DeserializeObject<Config>(file);
                if (config != null)
                {
                    return config;
                }
                else
                {
                    return new Config();
                }
            }
        }

        protected override int Run(string[] args)
        {
            SharedData.Config = GetConfig();
            LoadPlugins();
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStarted);

            int returns = 0;

            if (File.Exists("totals.bson"))
            {
                DataStatistician t = Utils.BsonDeserializeObject<DataStatistician>(File.ReadAllBytes("totals.bson")).ThrowIfNull();
                SharedData.DataStatistician = t;
            }
            else
            {
                using (var file = File.Create("totals.bson"))
                {
                    file.Write(Utils.BsonSerializeObject(SharedData.DataStatistician));
                }
            }

            // 从 .env.json 读取密钥然后 FetchToken
            ClusterInfo info = JsonConvert.DeserializeObject<ClusterInfo>(File.ReadAllTextAsync(".env.json").Result);
            SharedData.ClusterInfo = info;
            SharedData.Logger.LogSystem($"Cluster id: {info.ClusterID}");
            TokenManager token = new TokenManager(info);
            token.FetchToken().Wait();

            SharedData.Token = token;

            Cluster cluster = new(info, token);
            SharedData.Logger.LogSystem($"成功创建 Cluster 实例");
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Utils.ExitCluster(cluster).Wait();

            cluster.Start();
            cluster.WaitForStop();

            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStopped);
            return returns;
        }
    }
}
