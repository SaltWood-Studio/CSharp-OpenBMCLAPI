using System.Net.Sockets;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Client : IDisposable
    {
        public TcpClient TcpClient;
        Stream stream;

        public Stream Stream { get => this.stream; }

        public Client(TcpClient client, Stream stream)
        {
            this.TcpClient = client;
            this.stream = stream;
        }

        public void Close()
        {
            this.stream.Close();
            this.TcpClient.Close();
        }

        public void Dispose() => this.Close();

        public async Task<byte[]> Read(int n = 1)
        {
            byte[] buffer = new byte[n];
            long length = await this.stream.ReadAsync(buffer);
            byte[] data = new byte[length];
            Array.Copy(buffer, data, length);
            return data;
        }

        public async Task Write(byte[] data)
        {
            await this.stream.WriteAsync(data);
            await this.stream.FlushAsync();
        }

        public async Task CopyTo(Stream stream)
        {
            await this.stream.CopyToAsync(stream);
            await this.stream.FlushAsync();
        }

        public async Task CopyFrom(Stream stream)
        {
            await stream.CopyToAsync(this.stream);
            await stream.FlushAsync();
        }
    }
}
