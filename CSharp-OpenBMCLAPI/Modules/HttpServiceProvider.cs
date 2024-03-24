using CSharpOpenBMCLAPI.Modules.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TeraIO.Network.Http;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServiceProvider
    {
        /// <summary>
        /// 测速路由
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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
                StringValues ua;
                context.Request.Headers.TryGetValue("User-Agent", out ua);
                SharedData.Logger.LogInfo($"{context.Request.Method} {context.Request.Path} - [{context.Connection.RemoteIpAddress}] {ua.FirstOrDefault()}");
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

        /// <summary>
        /// 文件下载路由
        /// </summary>
        /// <param name="context"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public static async Task<FileAccessInfo> DownloadHash(HttpContext context, IStorage storage)
        {
            FileAccessInfo fai = default;
            if (!SharedData.Config.disableAccessLog)
            {
                StringValues ua;
                context.Request.Headers.TryGetValue("User-Agent", out ua);
                SharedData.Logger.LogInfo($"{context.Request.Method} {context.Request.Path} - [{context.Connection.RemoteIpAddress}] {ua.FirstOrDefault()}");
            }
            var pairs = Utils.GetQueryStrings(context.Request.QueryString.Value);
            string? hash = context.Request.Path.Value?.Split('/').LastOrDefault();
            string? s = pairs.GetValueOrDefault("s");
            string? e = pairs.GetValueOrDefault("e");

            bool valid = Utils.CheckSign(hash, SharedData.ClusterInfo.ClusterSecret, s, e);

            if (valid && hash != null && s != null && e != null)
            {
                try
                {
                    fai = await storage.Express(Utils.HashToFileName(hash), context);
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
            return fai;
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

        public static async Task Api(HttpContext context, string query, Cluster cluster)
        {
            switch (query)
            {
                case "today_hits":
                    await context.Response.WriteAsync("0");
                    break;
                case "today_bytes":
                    await context.Response.WriteAsync("0");
                    break;
                case "30d_hits":
                    await context.Response.WriteAsync("0");
                    break;
                case "30d_bytes":
                    await context.Response.WriteAsync("0");
                    break;
                case "status":
                    await context.Response.WriteAsync("正常");
                    break;
                case "uptime":
                    await context.Response.WriteAsync("0");
                    break;
                case "qps":
                    await context.Response.WriteAsync("""
                        [
                            {
                                "time": "14:58:00",
                                "average": 6,
                                "total": 30
                            }
                        ]
                        """);
                    break;
                case "1h_hits":
                    await context.Response.WriteAsync("""
                        [
                            {
                                "time": "0",
                                "io": 516,
                                "cache": 0
                            }
                        ]
                        """);
                    break;
                case "1h_bytes":
                case "1d_hits":
                case "1d_bytes":
                    // 同上
                    break;
            }
        }
    }
}
