using CSharpOpenBMCLAPI.Modules.Statistician;

namespace CSharpOpenBMCLAPI.Modules
{
    public class ClusterRequiredData
    {
        public static Config Config { get; set; } = new Config();
        public Logger Logger { get => Logger.Instance; }
        public ClusterInfo ClusterInfo { get; set; }
        public TokenManager Token { get; set; }
        public static DataStatistician DataStatistician { get; set; } = new DataStatistician();
        public SemaphoreSlim SemaphoreSlim { get; set; } = new SemaphoreSlim(0);

        internal int maxThreadCount;

        public ClusterRequiredData(ClusterInfo info)
        {
            this.ClusterInfo = info;
            this.Token = new(info);
        }
    }
}
