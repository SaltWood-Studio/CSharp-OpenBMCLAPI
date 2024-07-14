namespace CSharpOpenBMCLAPI.Modules
{
    internal class HttpRequest
    {

        public static HttpClient client;


        static HttpRequest()
        {
            client = new HttpClient()
            {
                // 设置基础地址，根据配置文件中的StagingMode属性判断使用哪个地址
                BaseAddress = ClusterRequiredData.Config.StagingMode ?
                    new Uri("https://openbmclapi.staging.bangbang93.com/") :
                    new Uri("https://openbmclapi.bangbang93.com/"),


            };
            // 添加UserAgent，用于标识请求来源
            client.DefaultRequestHeaders.UserAgent.Add(new("openbmclapi-cluster", ClusterRequiredData.Config.clusterVersion));
        }
    }
}
