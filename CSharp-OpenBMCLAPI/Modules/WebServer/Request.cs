using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Request
    {
        string method;
        string path;
        string httpProtocol;
        Header header;
        int bodyLength;
        Client client;
        byte[] bodyData = new byte[]{};
        public Request(Client client, byte[] data) {
            this.client = client;
            byte[][] temp = WebUtils.SplitBytes(data, WebUtils.encode("\r\n\r\n")).ToArray();
            this.bodyData = temp[1];
            byte[][] requestHeader = WebUtils.SplitBytes(temp[0], WebUtils.encode("\r\n")).ToArray();
            temp = WebUtils.SplitBytes(requestHeader[0], WebUtils.encode(" "), 3).ToArray();
            (method, path, httpProtocol) = (WebUtils.decode(temp[0]), WebUtils.decode(temp[1]), WebUtils.decode(temp[2]));
            Array.Copy(requestHeader, 1, requestHeader, 0, requestHeader.Length - 1);
            header = Header.fromBytes(requestHeader);
            bodyLength = int.Parse(header.get("Content-Length", 0) + "");
        }
        public async Task skipContent()
        {
            await this.client.Read(bodyLength - bodyData.Length);
        }
    }
}
