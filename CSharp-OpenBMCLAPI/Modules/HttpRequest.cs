using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            client.DefaultRequestHeaders.Add("User-Agent", "openbmclapi-cluster/1.9.7");
        }
    }
}
