using CSharpOpenBMCLAPI.Modules.Storage;
using CSharpOpenBMCLAPI.Modules.WebServer;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        public static Task LogAndRun(HttpContext context, Action action)
        {
            action.Invoke();
            if (!ClusterRequiredData.Config.disableAccessLog)
            {
                Logger.Instance.LogInfo($"{context.Request.Method} {context.Request.Path} <{context.Response.StatusCode}> - [{context.RemoteIPAddress}] {context.Request.Header.TryGetValue("User-Agent")}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 测速路由
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task Measure(HttpContext context, Cluster cluster)
        {
            var pairs = Utils.GetQueryStrings(context.Request.QueryString);
            bool valid = Utils.CheckSign(context.Request.Path
                , cluster.requiredData.ClusterInfo.ClusterSecret
                , pairs.GetValueOrDefault("s")
                , pairs.GetValueOrDefault("e")
            );
            if (valid)
            {
                context.Response.StatusCode = 200;
                byte[] buffer = new byte[1024];
                for (int i = 0; i < Convert.ToInt32(context.Request.Path.Split('/').LastOrDefault()); i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        await context.Response.Stream.WriteAsync(buffer);
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
        public static async Task<FileAccessInfo> DownloadHash(HttpContext context, Cluster cluster)
        {
            FileAccessInfo fai = default;
            var pairs = Utils.GetQueryStrings(context.Request.QueryString);
            string? hash = context.Request.Path.Split('/').LastOrDefault();
            string? s = pairs.GetValueOrDefault("s");
            string? e = pairs.GetValueOrDefault("e");

            bool valid = Utils.CheckSign(hash, cluster.clusterInfo.ClusterSecret, s, e);

            if (valid && hash != null && s != null && e != null)
            {
                try
                {
                    fai = await cluster.storage.Express(Utils.HashToFileName(hash), context);
                    ClusterRequiredData.DataStatistician.DownloadCount(fai);
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
            context.Response.Header.Set("Content-Type", "application/json");
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
        }

        public static void Dashboard(HttpContext context, string filePath = "/index.html")
        {
            context.Response.SendFileAsync(Path.Combine(Environment.CurrentDirectory, $"Dashboard{filePath}")).Wait();
        }
    }
}
