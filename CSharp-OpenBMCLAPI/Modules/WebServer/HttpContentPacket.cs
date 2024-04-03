using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class HttpContentPacket
    {
        protected long startTime;
        protected byte[] info;
        protected byte[] body;
        protected EndPoint endpoint;

        public HttpContentPacket()
        {

        }

        [Obsolete($"Please use {nameof(HttpContentPacket.Create)} instead.")]
        public HttpContentPacket(byte[] data)
        {

        }

        public static HttpContentPacket? Create(byte[] bytes, TcpClient client)
        {
            // = bytes.Split("\r\n")
            HttpContentPacket result = new()
            {
                startTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                //info
                //body = ,
                endpoint = client.Client.RemoteEndPoint.ThrowIfNull()
            };
            return null;
        }

#error 未完成代码，不要编译
    }
}