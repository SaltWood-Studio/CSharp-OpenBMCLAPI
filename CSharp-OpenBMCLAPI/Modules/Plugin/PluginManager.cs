using CSharpOpenBMCLAPI.Modules.WebServer;
using System.Diagnostics;

namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    public class PluginManager
    {
        private List<PluginHttpEvent> events = new List<PluginHttpEvent>();
        private List<PluginBase> plugins = new List<PluginBase>();
        private static PluginManager _instance = new PluginManager();
        public static PluginManager Instance { get => _instance; }

        /// <summary>
        /// 私有构造器，保证只有一个实例
        /// </summary>
        private PluginManager()
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

                Logger.Instance.LogInfo($"插件 {plugin} 已成功加载！");
            }
            catch (InvalidOperationException)
            {
                Logger.Instance.LogError($"无法执行的操作：在不允许的位置调用了 {nameof(RegisterPlugin)}：{caller}！");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"注册插件 {plugin} 时出现错误（阶段 1，添加插件）：", ex.ExceptionToDetail());
            }
        }

        public void RegisterHttpEvent(PluginHttpEvent e)
        {
            string? caller = null;
            try
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1).ThrowIfNull();
                caller = sf.GetMethod()?.Name;

                events.Add(e);
            }
            catch (InvalidOperationException)
            {
                Logger.Instance.LogError($"无法执行的操作：在不允许的位置调用了 {nameof(RegisterHttpEvent)}：{caller}！");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"注册事件时出现错误：", ex.ExceptionToDetail());
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
                Logger.Instance.LogError("注册插件时出现错误（阶段 0，实例化）：", ex.ExceptionToDetail());
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

        public void TriggerHttpEvent(HttpContext context, HttpEventType eventType)
        {
            events.ForEach(e => e.Trigger(eventType, context));
        }
    }
}
