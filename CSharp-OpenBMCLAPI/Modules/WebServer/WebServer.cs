using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    class WebServer : RunnableBase
    {
        private int Port = 0; // TCP 随机端口  
        private readonly X509Certificate2? _certificate; // SSL证书  
        private readonly int buffer = 8192;

        public WebServer(int port, X509Certificate2? certificate)
        {
            Port = port;
            _certificate = certificate;
        }

        protected override int Run(string[] args)
        {
            return AsyncRun().Result;
        }

        protected async Task<int> AsyncRun()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, Port);
            listener.Start();

            HttpListener httpListener = new();
            Console.WriteLine("已监听");

            while (true)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Accepted ");
                Stream stream = tcpClient.GetStream();
                if (_certificate != null)
                {
                    stream = new SslStream(stream, false, ValidateServerCertificate, null);

                }
                Console.WriteLine("已Handle");
                await Handle(new Client(tcpClient, stream));
            }
        }
        protected async Task Handle(Client client)
        {
            try
            {
                byte[] buf = await client.Read(this.buffer);
                Request request = new Request(client, buf);
            } catch (Exception e)
            {
                e.PrintTypeInfo();
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return _certificate != null;
        }
        public static void printBytes(byte[] bytes)
        {
            string text = "";
            foreach (var byte_ in bytes)
            {
                text += byteToString(byte_);
            }
            Console.WriteLine(text);
        }
        public static string byteToString(byte hex)
        {
            return hex <= 8 || (hex >= 11 && hex <= 12) || (hex >= 14 && hex <= 31) || (hex >= 127 && hex <= 255) ? "\\x" + BitConverter.ToString(new byte[] { hex }) : (hex == 9 ? "\\t" : (hex == 10 ? "\\n" : (hex == 13 ? "\\r" : Encoding.ASCII.GetString(new byte[] { hex }))));
        }
        /*
         * By TTB
         *  function Socket_tostring(hex) {
                hex = hex < 0 ? hex + 256 : hex
                return hex <= 8 || (hex >= 11 && hex <= 12) || (hex >= 14 && hex <= 31) || (hex >= 127 && hex <= 255) ? "\\x" + format("%02x", hex) : (hex == 9 ? "\\t" : (hex == 10 ? "\\n" : (hex == 13 ? "\\r" : hex.tochar())))
            }
         */
    }
}
