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
    class SimpleWebServer : RunnableBase
    {
        private const int Port = 443; // HTTPS默认端口  
        private readonly X509Certificate2 _certificate; // SSL证书  

        public SimpleWebServer(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        protected override int Run(string[] args)
        {
            int result = 0;
            result = AsyncRun().Result;
            return result;
        }

        protected async Task<int> AsyncRun()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, Port);
            listener.Start();

            HttpListener httpListener = new();

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                SslStream sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

                try
                {
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.None, false);

                    byte[] responseBytes = Array.Empty<byte>();

                    HttpContentPacket packet = HttpContentPacket.Create(responseBytes);

                    await sslStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    sslStream.Close();
                    client.Close();
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                }
                finally
                {
                    sslStream.Close();
                    client.Close();
                }
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}