using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Extension;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Response
    {
        public int StatusCode { get; set; } = 200;
        public Header Header { get; set; } = new Header();
        public Stream Stream { get; set; } = new MemoryStream();

        public async Task Call(Client client, Request request)
        {
            Header.Set("Content-Length", Stream.Length);
            Header.Set("Server", "CSharp-SaltWood");
            string responseHeader = $"HTTP/1.1 {StatusCode} {GetStatusMsg(StatusCode)}\r\n{Header}\r\n";
            await client.Write(responseHeader.Encode());
            Stream.CopyTo(client.Stream);
        }

        public static readonly Dictionary<int, string> STATUS_CODES = new Dictionary<int, string>
        {
            { 100, "Continue" },
            { 101, "Switching Protocols" },
            { 200, "OK" },
            { 201, "Created" },
            { 202, "Accepted" },
            { 203, "Non-Authoritative Information" },
            { 204, "No Content" },
            { 205, "Reset Content" },
            { 206, "Partial Content" },
            { 300, "Multiple Choices" },
            { 301, "Moved Pemanently" },
            { 302, "Found" },
            { 303, "See Other" },
            { 304, "Not Modified" },
            { 305, "Use Proxy" },
            { 306, "Unused" },
            { 307, "Temporary Redirect" },
            { 400, "Bad Request" },
            { 401, "Unauthorized" },
            { 402, "Payment Required" },
            { 403, "Forbidden" },
            { 404, "Not Found" },
            { 405, "Method Not Allowed" },
            { 406, "Not Acceptable" },
            { 407, "Proxy Authentication Required" },
            { 408, "Request Time-out" },
            { 409, "Conflict" },
            { 410, "Gone" },
            { 411, "Length Required" },
            { 412, "Precondition Failed" },
            { 413, "Request Entity Too Large" },
            { 414, "Request-URI Too Large" },
            { 415, "Unsupported Media Type" },
            { 416, "Requested range not satisfiable" },
            { 417, "Expectation Failed" },
            { 418, "I'm a teapot" },
            { 421, "Misdirected Request" },
            { 422, "Unprocessable Entity" },
            { 423, "Locked" },
            { 424, "Failed Dependency" },
            { 425, "Too Early" },
            { 426, "Upgrade Required" },
            { 428, "Precondition Required" },
            { 429, "Too Many Requests" },
            { 431, "Request Header Fields Too Large" },
            { 451, "Unavailable For Legal Reasons" },
            { 500, "Internal Server Eror" },
            { 501, "Not Implemented" },
            { 502, "Bad Gateway" },
            { 503, "Service Unavailable" },
            { 504, "Gateway Time-out" },
            { 505, "HTTP Version not supported" },
        };

        public static string GetStatusMsg(int status)
        {
            return STATUS_CODES.ContainsKey(status) ? STATUS_CODES[status] : STATUS_CODES[int.Parse((status / 100) + "") * 100];
        }
    }
}
