using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class HttpContext
    {
        public required Request Request { get; set; }
        public required Response Response { get; set; }
        public required IPEndPoint RemoteIPAddress { get;  set; }
    }
}
