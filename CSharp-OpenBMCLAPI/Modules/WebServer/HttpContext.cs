using System.Net;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class HttpContext
    {
        public required Request Request { get; set; }
        public required Response Response { get; set; }
        public required EndPoint RemoteIPAddress { get; set; }
    }
}
