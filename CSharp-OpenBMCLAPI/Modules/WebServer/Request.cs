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
        string method = "GET";
        string path = "/";
        string httpProtocol = "HTTP/1.1";
        public Request(Client client, byte[] data) {
            byte[][] temp = WebUtils.SplitBytes(data, encode("\r\n\r\n")).ToArray();
            byte[][] headers = WebUtils.SplitBytes(temp[0], encode("\r\n")).ToArray();
            temp = WebUtils.SplitBytes(headers[0], encode(" "), 2).ToArray();
            (method, path, httpProtocol) = (decode(temp[0]), decode(temp[1]), decode(temp[2]));
            Console.WriteLine((method, path, httpProtocol));
        }
        public string decode(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
        public byte[] encode(string data) {
            return Encoding.UTF8.GetBytes(data).ToArray();
        }
    }
}
