namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// 负责存储 Cluster 的 secret 和 id
    /// </summary>
    public struct ClusterInfo
    {
        public string ClusterID, ClusterSecret;

        public ClusterInfo(string id, string secret)
        {
            this.ClusterID = id;
            this.ClusterSecret = secret;
        }
    }
}
