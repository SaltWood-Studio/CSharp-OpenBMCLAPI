using CSharpOpenBMCLAPI.Modules;
using CSharpOpenBMCLAPI.Modules.Plugin;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI
{
    internal class Program : RunnableBase
    {
        public Program() : base() { }

        static void Main(string[] args)
        {
            SharedData.Logger.LogInfo($"Starting CSharp-OpenBMCLAPI v{SharedData.Config.clusterVersion}");
            SharedData.Logger.LogInfo("高性能、低メモリ占有！");
            SharedData.Logger.LogInfo($"运行时环境：{Utils.GetRuntime()}");
            Program program = new Program();
            program.Start();
            program.WaitForStop();
        }

        protected override int Run(string[] args)
        {
            SharedData.Config = GetConfig();
            LoadPlugins();
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStarted);
            Task<int> task = AsyncRun(args);
            task.Wait();
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStopped);
            return task.Result;
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
                                SharedData.PluginManager.RegisterPlugin(type);
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

        protected async Task<int> AsyncRun(string[] args)
        {
            int returns = 0;

            /*
            if (!Utils.IsAdministrator())
            {
                bool success = Utils.RunAsAdministrator();
                if (success)
                {
                    Environment.Exit(0);
                }
                else
                {
                    SharedData.Logger.LogWarn("用户拒绝了管理员权限，集群可能无法正常运行！");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Utils.CreatePortRule("CSharp-OpenBMCLAPI",
                    SharedData.Config.PORT,
                    WindowsFirewallHelper.FirewallAction.Allow,
                    WindowsFirewallHelper.FirewallDirection.Inbound
                );
            }*/

            // 从 .env.json 读取密钥然后 FetchToken
            ClusterInfo info = JsonConvert.DeserializeObject<ClusterInfo>(await File.ReadAllTextAsync(".env.json"));
            SharedData.ClusterInfo = info;
            SharedData.Logger.LogInfo($"Cluster id: {info.ClusterID}");
            TokenManager token = new TokenManager(info);
            await token.FetchToken();

            SharedData.Token = token;

            Cluster cluster = new(info, token);
            SharedData.Logger.LogInfo($"成功创建 Cluster 实例");
            AppDomain.CurrentDomain.ProcessExit += async (sender, e) => await Utils.ExitCluster(cluster);
            cluster.Start();
            cluster.WaitForStop();

            return returns;
        }
    }
}
