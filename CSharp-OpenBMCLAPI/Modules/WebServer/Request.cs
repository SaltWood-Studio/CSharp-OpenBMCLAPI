using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Request
    {
        internal string? QueryString = "";

        public string Method { get; set; }
        public string Path { get; set; }
        public string Protocol { get; set; }
        public Header Header { get; set; }
        public int BodyLength { get; set; }
        public Client Client { get; set; }
        public byte[] BodyData { get; set; }

        public Request(Client client, byte[] data)
        {
            this.Client = client;
            byte[][] temp = WebUtils.SplitBytes(data, WebUtils.Encode("\r\n\r\n"), 2).ToArray();
            this.BodyData = temp[1];
            byte[][] requestHeader = WebUtils.SplitBytes(temp[0], WebUtils.Encode("\r\n")).ToArray();
            temp = WebUtils.SplitBytes(requestHeader[0], WebUtils.Encode(" "), 3).ToArray();
            (Method, Path, Protocol) = (WebUtils.Decode(temp[0]), WebUtils.Decode(temp[1]), WebUtils.Decode(temp[2]));
            Header = Header.FromBytes(requestHeader[1..]);
            BodyLength = int.Parse(Header.Get("content-length", 0) + "");
        }

        public async Task SkipContent()
        {
            await this.Client.Read(BodyLength - BodyData.Length);
        }

        public override string ToString()
        {
            string result = $"""
                {Method} {Path} {Protocol}

                Headers:
                    {string.Join("\n    ", this.Header.Select(f => $"{f.Key}: {f.Value}"))}

                Data:
                
                {Convert.ToHexString(this.BodyData)}
                """;
            return result;
        }
    }
}
