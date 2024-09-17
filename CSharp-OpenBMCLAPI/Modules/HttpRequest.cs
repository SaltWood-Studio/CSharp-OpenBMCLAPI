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
            //string ua = $"openbmclapi-cluster/{ClusterRequiredData.Config.clusterVersion} (CSharp-OpenBMCLAPI; .NET runtime v{Environment.Version}; {Environment.OSVersion}, {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}; {System.Globalization.CultureInfo.InstalledUICulture.Name})";
            string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0";
            client.DefaultRequestHeaders.Add("User-Agent", ua);
            Console.WriteLine($"User-Agent: {ua}");
        }
    }
}
