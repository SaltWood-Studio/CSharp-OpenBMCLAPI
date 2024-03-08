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
            var a = req.Request;
            Debugger.Break();
        }

        public void Load() => this.LoadNew();
    }
}
