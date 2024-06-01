using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class SimpleWebServer : RunnableBase
    {
        private int Port = 0; // TCP 随机端口  
        private readonly X509Certificate2? _certificate; // SSL证书  
        private Cluster cluster;
        public readonly List<Route> routes = new();
        private readonly int bufferSize = 8192;

        public SimpleWebServer(int port, X509Certificate2? certificate, Cluster cluster)
        {
            Port = port;
            _certificate = certificate;
            this.cluster = cluster;
        }

        protected override int Run(string[] args)
        {
            while (true)
            {
                int result = -1;
                try
                {
                    result = AsyncRun().Result;
                    return result;
                }
                catch (Exception ex)
                {
                    ex.GetType();
                    Logger.Instance.LogError(ex.ExceptionToDetail());
                    return result;
                }
            }
        }

        protected async Task<int> AsyncRun()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            HttpListener httpListener = new();

            while (true)
            {
                TcpClient? tcpClient = null;
                try
                {
                    tcpClient = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleRequest(tcpClient));
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError(ex.ExceptionToDetail());
                    if (tcpClient != null && tcpClient.Connected)
                    {
                        tcpClient?.Close();
                    }
                }
            }
        }

        protected void HandleRequest(TcpClient tcpClient)
        {
            Stream stream = tcpClient.GetStream();
            if (_certificate != null)
            {
                SslStream sslStream = new SslStream(stream, false, ValidateServerCertificate, null);
                sslStream.AuthenticateAsServer(this._certificate, false, false);
                stream = sslStream;
            }
            if (Handle(new Client(tcpClient, stream)).Result && tcpClient.Connected)
            {
                stream.Close();
                tcpClient.Close();
            }
        }

        protected async Task<bool> Handle(Client client) // 返回值代表是否关闭连接？
        {

            byte[] buffer = await client.Read(this.bufferSize);
            var str = Encoding.UTF8.GetString(buffer);
            Request request = new Request(client, buffer);
            Response response = new Response();
            HttpContext context = new HttpContext() { Request = request, RemoteIPAddress = client.TcpClient.Client.RemoteEndPoint!, Response = response };

            foreach (Route route in this.routes)
            {
                if (route.MatchRegex.Match(context.Request.Path).Success)
                {
                    if (!route.Methods.Contains(context.Request.Method))
                    {
                        context.Response.StatusCode = 405;
                        await context.Response.WriteAsync("405 Method Not Allowed");
                        context.Response.ResetStreamPosition();
                        break;
                    }
                    foreach (var func in route.ConditionExpressions)
                    {
                        bool result = func.Invoke(context.Request.Path);
                        if (!result) goto NextOne;
                    }
                    // 已经判断符合所有条件

                    route.Handler?.Invoke(context, cluster, route.MatchRegex.Match(context.Request.Path));
                    break;
                }
            NextOne: continue;
            }

            await response.Call(client, request); // 可以多次调用Response

            //SharedData.Logger.LogInfo($"{request.Method} {request.Path} <{response.StatusCode}> - [{request.Client.TcpClient.Client.RemoteEndPoint}] {request.Header.TryGetValue("User-Agent")}");

            return true;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return _certificate != null;
        }

        private static void PrintBytes(byte[] bytes)
        {
            string text = "";
            foreach (var byte_ in bytes)
            {
                text += ByteToString(byte_);
            }
            Console.WriteLine(text);
        }

        private static void PrintArrayBytes(byte[][] bytes)
        {
            bytes.ForEach(e => PrintBytes(e));
        }

        private static string ByteToString(byte hex)
        {
            return hex <= 8 || (hex >= 11 && hex <= 12) || (hex >= 14 && hex <= 31) || (hex >= 127 && hex <= 255) ? "\\x" + BitConverter.ToString(new byte[] { hex }) : (hex == 9 ? "\\t" : (hex == 10 ? "\\n" : (hex == 13 ? "\\r" : Encoding.ASCII.GetString(new byte[] { hex }))));
        }
    }
}
