using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Network.Http;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServer : HttpServerAppBase
    {
        public HttpServer() : base() => LoadNew();

        [HttpHandler("/measure")]
        public void Measure(HttpClientRequest req)
        {
            string? url = req.Request.RawUrl?.Split('?').FirstOrDefault();

            Dictionary<string, string> pairs = ParseRequest(req);
            bool valid = Utils.CheckSign(url, SharedData.ClusterInfo.ClusterSecret, pairs.GetValueOrDefault("s"), pairs.GetValueOrDefault("e"));
            if (valid)
            {
                byte[] bytes = new byte[1024];
                for (int i = 0; i < Convert.ToInt32(url?.Split('/').LastOrDefault()); i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        req.ResponseStatusCode = 200;
                        req.Send(bytes);
                    }
                }
                req.Response.Close();
            }
            else
            {
                req.ResponseStatusCode = 403;
                req.Send($"Access to \"{req.Request.RawUrl}\" has been blocked due to your request timeout or invalidity.");
            }
        }

        [HttpHandler("/download/")]
        public void DownloadHash(HttpClientRequest req)
        {
            var pairs = ParseRequest(req);
            string? hash = req.Request.RawUrl?.Split('?').FirstOrDefault()?.Split('/').LastOrDefault();
            string? s = pairs.GetValueOrDefault("s");
            string? e = pairs.GetValueOrDefault("e");

            bool valid = Utils.CheckSign(hash, SharedData.ClusterInfo.ClusterSecret, s, e);

            if (valid && hash != null && s != null && e != null)
            {
                string? range = req.Request.Headers.GetValues("Range")?.FirstOrDefault();
                try
                {
                    if (range != null)
                    {
                        (int from, int to) = (Convert.ToInt32(range?.Split('-')[0]), Convert.ToInt32(range?.Split('-')[1]));
                        using var file = File.OpenRead($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
                        file.Position = from;
                        byte[] buffer = new byte[to - from + 1];
                        file.Read(buffer, 0, to - from + 1);
                        req.ResponseStatusCode = 206;
                        req.Send(buffer);
                    }
                    else
                    {
                        using var file = File.OpenRead($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
                        file.CopyTo(req.OutputStream);
                    }
                }
                catch
                {
                    var destination = req.Request.Headers.GetValues("Referer")?.FirstOrDefault();
                    if (destination != null)
                    {
                        req.ResponseStatusCode = 302;
                        req.Response.AddHeader("Location", destination);
                    }
                    else
                    {
                        req.Response.Close();
                    }
                }
            }
            else
            {
                req.ResponseStatusCode = 403;
                req.Send($"Access to \"{req.Request.RawUrl}\" has been blocked due to your request timeout or invalidity.");
            }
        }

        private static Dictionary<string, string> ParseRequest(HttpClientRequest req)
        {
            Dictionary<string, string> pairs = new();
            if (req.Request.RawUrl != null)
            {
                foreach ((string? key, string? value) in req.Request.RawUrl.Split('?').Last().Split('&').Select(param => (param.Split('=').FirstOrDefault(), param.Split('=').LastOrDefault())))
                {
                    if (key != null && value != null) pairs[key] = value;
                }
            }
            return pairs;
        }
    }
}
