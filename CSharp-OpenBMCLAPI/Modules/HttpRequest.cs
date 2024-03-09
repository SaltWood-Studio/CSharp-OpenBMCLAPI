namespace CSharpOpenBMCLAPI.Modules
{
    internal class HttpRequest
    {
        public static HttpClient client;

        static HttpRequest()
        {
            client = new HttpClient()
            {
                BaseAddress = new Uri("https://openbmclapi.bangbang93.com/")
            };
            client.DefaultRequestHeaders.UserAgent.Add(new("openbmclapi-cluster", SharedData.Config.clusterVersion));
        }
    }
}
