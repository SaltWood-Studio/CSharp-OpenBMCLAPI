using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TeraIO.Network.Http;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServerUtils
    {
        public static Task Measure(HttpContext context)
        {
            var pairs = Utils.GetQueryStrings(context.Request.QueryString.Value);
            bool valid = Utils.CheckSign(context.Request.Path.Value?.Split('/').LastOrDefault()
                , SharedData.ClusterInfo.ClusterSecret
                , pairs.GetValueOrDefault("s")
                , pairs.GetValueOrDefault("e")
            );
            if (valid)
            {
                byte[] bytes = new byte[1024];
                for (int i = 0; i < Convert.ToInt32(context.Request.Path.Value?.Split('/').LastOrDefault()); i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.Body.Write(bytes);
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                context.Response.Body.Write(Encoding.UTF8.GetBytes($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity."));
            }

            return Task.CompletedTask;
        }

        [HttpHandler("/download/")]
        public static Task DownloadHash(HttpContext context)
        {
            var pairs = Utils.GetQueryStrings(context.Request.QueryString.Value);
            string? hash = context.Request.Path.Value?.Split('/').LastOrDefault();
            string? s = pairs.GetValueOrDefault("s");
            string? e = pairs.GetValueOrDefault("e");

            bool valid = Utils.CheckSign(hash, SharedData.ClusterInfo.ClusterSecret, s, e);

            if (valid && hash != null && s != null && e != null)
            {
                StringValues value;
                context.Request.Headers.TryGetValue("Range", out value);

                string? range = value.FirstOrDefault();
                try
                {
                    if (range != null)
                    {
                        (int from, int to) = (Convert.ToInt32(range?.Split('-')[0]), Convert.ToInt32(range?.Split('-')[1]));
                        using var file = File.OpenRead($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
                        file.Position = from;
                        byte[] buffer = new byte[to - from + 1];
                        file.Read(buffer, 0, to - from + 1);
                        context.Response.StatusCode = 206;
                        context.Response.Body.Write(buffer);
                    }
                    else
                    {
                        context.Response.SendFileAsync($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}").Wait();
                    }
                }
                catch
                {
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                context.Response.Body.Write(Encoding.UTF8.GetBytes($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity."));
            }

            return Task.CompletedTask;
        }
    }
}
