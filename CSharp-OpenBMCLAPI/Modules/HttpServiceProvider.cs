using CSharpOpenBMCLAPI.Modules.Storage;
using CSharpOpenBMCLAPI.Modules.WebServer;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TeraIO.Extension;
using TeraIO.Network.Http;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI.Modules
{
    public class HttpServiceProvider
    {
        /// <summary>
        /// 记录访问日志并且调用指定的 action
        /// </summary>
        /// <param name="context"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static void LogAccess(HttpContext context)
        {
            if (!ClusterRequiredData.Config.disableAccessLog)
            {
                Logger.Instance.LogInfo($"{context.Request.Method} {context.Request.Path.Split('?').First()} <{context.Response.StatusCode}> - [{context.RemoteIPAddress}] {context.Request.Header.TryGetValue("user-agent")}");
            }
        }

        /// <summary>
        /// 测速路由
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task Measure(HttpContext context, Cluster cluster)
        {
            var pairs = Utils.GetQueryStrings(context.Request.Path.Split('?').Last());
            bool valid = Utils.CheckSign(context.Request.Path.Split('?').First()
                , cluster.requiredData.ClusterInfo.ClusterSecret
                , pairs.GetValueOrDefault("s")
                , pairs.GetValueOrDefault("e")
            );
            if (valid)
            {
                context.Response.StatusCode = 200;
                byte[] buffer = new byte[1024];
                for (int i = 0; i < Convert.ToInt32(context.Request.Path.Split('/').Last().Split('?').First()); i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        await context.Response.Stream.WriteAsync(buffer);
                    }
                }
                context.Response.ResetStreamPosition();
            }
            else
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity.");
            }
            LogAccess(context);
        }

        /// <summary>
        /// 文件下载路由
        /// </summary>
        /// <param name="context"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public static async Task<FileAccessInfo> DownloadHash(HttpContext context, Cluster cluster)
        {
            FileAccessInfo fai = default;
            var pairs = Utils.GetQueryStrings(context.Request.Path.Split('?').Last());
            string? hash = context.Request.Path.Split('/').LastOrDefault()?.Split('?').First();
            string? s = pairs.GetValueOrDefault("s");
            string? e = pairs.GetValueOrDefault("e");

            bool valid = Utils.CheckSign(hash, cluster.clusterInfo.ClusterSecret, s, e);

            if (valid && hash != null && s != null && e != null)
            {
                long from, to;
                try
                {
                    if (context.Request.Header.ContainsKey("range"))
                    {
                        // 206 处理部分
                        context.Response.StatusCode = 206;
                        (from, to) = ToRangeByte(context.Request.Header["range"].Split("=").Last().Split("-"));
                        if (to < from && to != -1) (from, to) = (to, from);

                        //TODO: 尝试优化这坨屎
                        Stream file = cluster.storage.ReadFileStream(Utils.HashToFileName(hash));
                        if (to == -1) to = file.Length;

                        long length = (to - from + 1);
                        context.Response.Header["Content-Length"] = length.ToString();

                        file.Seek(from, SeekOrigin.Begin);
                        for (long i = from; i <= to; i++)
                        {
                            context.Response.Stream.WriteByte((byte)file.ReadByte());
                        }
                        context.Response.ResetStreamPosition();

                        context.Response.Header["Content-Range"] = $"{from}-{to}/{context.Response.Stream.Length}";
                        context.Response.Header["x-bmclapi-hash"] = hash;
                        context.Response.Header["Accept-Ranges"] = "bytes";
                        context.Response.Header["Content-Type"] = "application/octet-stream";
                        context.Response.Header["Connection"] = "closed";
                        fai = new FileAccessInfo
                        {
                            hits = 1,
                            bytes = length
                        };
                        ClusterRequiredData.DataStatistician.DownloadCount(fai);
                    }
                    else
                    {
                        fai = await cluster.storage.HandleRequest(Utils.HashToFileName(hash), context);
                        context.Response.ResetStreamPosition();
                        ClusterRequiredData.DataStatistician.DownloadCount(fai);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError(ex.ExceptionToDetail());
                    //Logger.Instance.LogError(ex.StackTrace);
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                context.Response.Header.Remove("Content-Length");
                await context.Response.WriteAsync($"Access to \"{context.Request.Path}\" has been blocked due to your request timeout or invalidity.");
            }
            LogAccess(context);
            return fai;
        }

        private static (long from, long to) ToRangeByte(string[]? rangeHeader)
        {
            int from, to;
            if (string.IsNullOrWhiteSpace(rangeHeader?.FirstOrDefault()))
                from = 0;
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
            context.Response.Header.Set("content-type", "application/json");
            context.Response.StatusCode = 200;
            switch (query)
            {
                case "qps":
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(ClusterRequiredData.DataStatistician.Qps));
                    break;
                case "dashboard":
                    // SharedData.Logger.LogDebug(JsonConvert.SerializeObject(SharedData.DataStatistician.Dashboard));
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(ClusterRequiredData.DataStatistician.Dashboard));
                    break;
                case "system":
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        memory = ClusterRequiredData.DataStatistician.Memory,
                        connections = ClusterRequiredData.DataStatistician.Connections,
                        cpu = ClusterRequiredData.DataStatistician.Cpu,
                        cache = new
                        {
                            total = (cluster.storage as ICachedStorage != null) ? ((ICachedStorage)cluster.storage).GetCachedFiles() : 0,
                            bytes = (cluster.storage as ICachedStorage != null) ? ((ICachedStorage)cluster.storage).GetCachedMemory() : 0
                        }
                    }));
                    break;
                case "status":
                    await context.Response.WriteAsync(cluster.IsEnabled ? "好闲啊o(*￣▽￣*)ブ" : "似了w(ﾟДﾟ)w");
                    break;
                case "uptime":
                    await context.Response.WriteAsync(ClusterRequiredData.DataStatistician.Uptime.ToString("0.00"));
                    break;
            }
            context.Response.ResetStreamPosition();
        }

        public static Task Dashboard(HttpContext context, string filePath = "index.html")
        {
            context.Response.StatusCode = 200;
            context.Response.Stream = Utils.GetEmbeddedFileStream($"Dashboard/{filePath}").ThrowIfNull();
            return Task.CompletedTask;
        }
    }
}
