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
    [Obsolete("懒得写了")]
    public class HttpContentPacket
    {
        //protected long startTime;
        //protected byte[] info;
        //protected byte[] body;
        //protected EndPoint endpoint;
        //protected string useragent;
        //protected Dictionary<string, string> headers;


        //public HttpContentPacket(byte[] bytes, TcpClient client)
        //{
            //startTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            //endpoint = client.Client.RemoteEndPoint.ThrowIfNull();
            //List<byte[]> list = bytes.Split("\r\n", 2);
            //info = list[0];
            //body = list[1];
            //headers = ParseToHeaders(list[2]);
        //}

        //private Dictionary<string, string> ParseToHeaders(byte[] bytes)
        //{
        //
        //}

        // #error 未完成代码，不要编译
    }
}