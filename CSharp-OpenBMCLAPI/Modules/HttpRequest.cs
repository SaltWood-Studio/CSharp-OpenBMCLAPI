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
                BaseAddress = new Uri(PublicData.Config.CenterServerAddress),
                Timeout = TimeSpan.FromMinutes(5)
            };
            // 添加UserAgent，用于标识请求来源
            string ua = $"openbmclapi-cluster/{PublicData.Config.clusterVersion} (CSharp-OpenBMCLAPI; .NET runtime v{Environment.Version}; {Environment.OSVersion}, {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}; {System.Globalization.CultureInfo.InstalledUICulture.Name})";
            client.DefaultRequestHeaders.Add("User-Agent", ua);
            Console.WriteLine($"User-Agent: {ua}");
        }
    }
}
