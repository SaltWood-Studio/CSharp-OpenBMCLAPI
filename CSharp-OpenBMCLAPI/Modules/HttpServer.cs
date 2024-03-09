using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TeraIO.Network.Http;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServerUtils
    {
        public static async Task Measure(HttpContext context)
        {
            var pairs = Utils.GetQueryStrings(context.Request.QueryString.Value);
            bool valid = Utils.CheckSign(context.Request.Path.Value
                , SharedData.ClusterInfo.ClusterSecret
                , pairs.GetValueOrDefault("s")
                , pairs.GetValueOrDefault("e")
            );
            if (valid)
            {
                context.Response.StatusCode = 200;
                byte[] buffer = new byte[1024];
                for (int i = 0; i < Convert.ToInt32(context.Request.Path.Value?.Split('/').LastOrDefault()); i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        await context.Response.BodyWriter.WriteAsync(buffer);
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity.");
            }
        }

        public static async Task DownloadHash(HttpContext context)
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
                        range = range.Replace("bytes=", "");
                        var rangeHeader = range?.Split('-');
                        (long from, long to) = ToRangeByte(rangeHeader);
                        using var file = File.OpenRead($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
                        if (from == -1)
                            from = 0;
                        if (to == -1)
                            to = file.Length - 1;
                        file.Position = from;
                        byte[] buffer = new byte[to - from + 1];
                        file.Read(buffer, 0, (int)(to - from + 1));
                        context.Response.StatusCode = 206;
                        await context.Response.BodyWriter.WriteAsync(buffer);
                    }
                    else
                    {
                        await context.Response.SendFileAsync($"{SharedData.Config.clusterFileDirectory}cache/{hash[0..2]}/{hash}");
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
                await context.Response.WriteAsync($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity.");
            }
        }

        private static (long from, long to) ToRangeByte(string[]? rangeHeader)
        {
            int from, to;
            if (string.IsNullOrWhiteSpace(rangeHeader?.FirstOrDefault()))
                from = -1;
            else
                from = Convert.ToInt32(rangeHeader?.FirstOrDefault());
            if (string.IsNullOrWhiteSpace(rangeHeader?.LastOrDefault()))
                to = -1;
            else
                to = Convert.ToInt32(rangeHeader?.LastOrDefault());
            return (from, to);
        }
    }
}
