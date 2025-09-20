namespace CSharpOpenBMCLAPI.Modules
{
    public class PublicData(ClusterInfo info)
    {
        public static Config Config { get; set; } = new Config();
        public Logger Logger => Logger.Instance;
        public ClusterInfo ClusterInfo { get; set; } = info;
        public TokenManager Token { get; set; } = new(info);
        public SemaphoreSlim SemaphoreSlim { get; set; } = new SemaphoreSlim(0);

        internal int maxThreadCount;
    }
}
