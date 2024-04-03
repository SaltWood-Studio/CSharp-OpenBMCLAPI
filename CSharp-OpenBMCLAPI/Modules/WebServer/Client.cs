using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Client
    {
        TcpClient client;
        Stream stream;
        public Client(TcpClient client, Stream stream)
        {
            this.client = client;
            this.stream = stream;
        }
        public Stream GetStream() {
            return this.stream;
        }
        public void Close()
        {
            this.stream.Close();
            this.client.Close();
        }
        public async Task<byte[]> Read(int n = 1)
        {
            byte[] buffer = new byte[n];
            long length = await this.stream.ReadAsync(buffer);
            byte[] data = new byte[length];
            Array.Copy(buffer, data, length);
            return data;
        }
    }
}
