using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Network.Http;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServer : HttpServerAppBase
    {
        public HttpServer() : base() { }

        [HttpHandler("/measure")]
        public void Measure(HttpClientRequest req)
        {
            Dictionary<string, string> pairs = new();
            if (req.Request.RawUrl != null)
            {
                foreach ((string key, string value) in req.Request.RawUrl.Split('?').Last().Split('&').Select(param => (param.Split('=')[0], param.Split('=')[1])))
                {
                    pairs[key] = value;
                }
                bool valid = Utils.CheckSign(req.Request.RawUrl, SharedData.ClusterInfo.ClusterSecret, pairs.GetValueOrDefault("s"), pairs.GetValueOrDefault("e"));
                if (valid)
                {
                    byte[] bytes = new byte[1024];
                    for (int i = 0; i < Convert.ToInt32(pairs.GetValueOrDefault("e"), 36); i++)
                    {
                        for (int j = 0; j < 1024; j++)
                        {
                            req.Send(bytes);
                        }
                    }
                }
            }
        }

        public void Load() => this.LoadNew();
    }
}
