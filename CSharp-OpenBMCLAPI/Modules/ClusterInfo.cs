namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// 负责存储 Cluster 的 secret 和 id
    /// </summary>
    public readonly struct ClusterInfo(string id, string secret)
    {
        public readonly string clusterId = id;
        public readonly string clusterSecret = secret;
    }
}
