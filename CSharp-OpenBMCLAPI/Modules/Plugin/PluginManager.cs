using log4net.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    public class PluginManager
    {
        private List<PluginBase> plugins = new List<PluginBase>();

        public PluginManager()
        {

        }

        public void RegisterPlugin(PluginBase plugin)
        {
            string? caller = null;
            try
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1).ThrowIfNull();
                caller = sf.GetMethod()?.Name;

                plugins.Add(plugin);

                SharedData.Logger.LogInfo($"插件 {plugin} 已成功加载！");
            }
            catch (InvalidOperationException)
            {
                SharedData.Logger.LogError($"无法执行的操作：在不允许的位置调用了 {nameof(RegisterPlugin)}：{caller}！");
            }
            catch (Exception ex)
            {
                SharedData.Logger.LogError($"注册插件 {plugin} 时出现错误（阶段 1，添加插件）：", Utils.ExceptionToDetail(ex));
            }
        }

        public void RegisterPlugin(Type type)
        {
            try
            {
                PluginBase plugin;

                plugin = (Activator.CreateInstance(type) as PluginBase).ThrowIfNull();

                RegisterPlugin(plugin);
            }
            catch (Exception ex)
            {
                SharedData.Logger.LogError("注册插件时出现错误（阶段 0，实例化）：", Utils.ExceptionToDetail(ex));
            }
        }

        public void TriggerEvent(object sender, ProgramEventType eventType)
        {
            Cluster? senderCluster = sender as Cluster;
            switch (eventType)
            {
                case ProgramEventType.ProgramStarted:
                    plugins.ForEach(p => p.OnProgramStarted());
                    break;
                case ProgramEventType.ProgramStopped:
                    plugins.ForEach(p => p.OnProgramStopped());
                    break;
                case ProgramEventType.ClusterStarted:
                    plugins.ForEach(p => p.OnClusterStarted(senderCluster));
                    break;
                case ProgramEventType.ClusterStopped:
                    plugins.ForEach(p => p.OnClusterStopped(senderCluster));
                    break;
            }
        }

        /*
        public void TriggerClientEvent(HttpContext sender, ClientEventType eventType)
        {
            switch (eventType)
            {
                case ClientEventType.ClientDownload:
                    events.ForEach(p => p.);
                    break;
                case ClientEventType.ClientMeasure:
                    events.ForEach(p => p.OnProgramStopped());
                    break;
                case ClientEventType.ClientOtherRequest:
                    events.ForEach(p => p.OnClusterStarted(senderCluster));
                    break;
            }
        }*/
    }
}
