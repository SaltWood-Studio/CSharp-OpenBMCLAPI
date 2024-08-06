namespace CSharpOpenBMCLAPI.Modules
{
    internal class HttpRequest
    {

        public static HttpClient client;


        static HttpRequest()
        {
            client = new HttpClient()
            {
                // 设置基础地址
                BaseAddress = new Uri(ClusterRequiredData.Config.CenterServerAddress)
            };
            // 添加UserAgent，用于标识请求来源
            client.DefaultRequestHeaders.Add("User-Agent", $"openbmclapi-cluster/{ClusterRequiredData.Config.clusterVersion} (CSharp-OpenBMCLAPI, dotnet CLR v{Environment.Version})");
        }
    }
}
