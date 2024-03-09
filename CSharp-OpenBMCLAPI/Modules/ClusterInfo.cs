namespace CSharpOpenBMCLAPI.Modules
{
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
