using CSharpOpenBMCLAPI.Modules.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;

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
            if (!ClusterRequiredData.Config.DisableAccessLog)
            {
                context.Request.Headers.TryGetValue("user-agent", out StringValues value);
                Logger.Instance.LogInfo($"{context.Request.Method} {context.Request.Path.Value} <{context.Response.StatusCode}> - [{context.Connection.RemoteIpAddress}] {value.FirstOrDefault()}");
            }
        }

        /// <summary>
        /// 测速路由
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task Measure(HttpContext context, Cluster cluster, int size)
        {
            context.Request.Query.TryGetValue("s", out StringValues s);
            context.Request.Query.TryGetValue("e", out StringValues e);
            bool valid = Utils.CheckSign(context.Request.Path.Value?.Split('?').First()
                , cluster.requiredData.ClusterInfo.ClusterSecret
                , s.FirstOrDefault()
                , e.FirstOrDefault()
            );
            if (valid)
            {
                context.Response.StatusCode = 200;
                byte[] buffer = new byte[1024];
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < 1024; j++)
                    {
                        await context.Response.Body.WriteAsync(buffer);
                    }
                }
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
        public static async Task<FileAccessInfo> DownloadHash(HttpContext context, Cluster cluster, string hash)
        {
            context.Request.Query.TryGetValue("s", out StringValues s);
            context.Request.Query.TryGetValue("e", out StringValues e);

            bool isValid = Utils.CheckSign(hash, cluster.clusterInfo.ClusterSecret, s.FirstOrDefault(), e.FirstOrDefault());

            if (!cluster.storage.Exists(Utils.HashToFileName(hash)))
            {
                LogAccess(context);
                context.Response.StatusCode = 404;
                return new FileAccessInfo();
            }

            // 获取文件信息
            using var stream = cluster.storage.ReadFileStream(Utils.HashToFileName(hash));
            long fileSize = cluster.storage.GetFileSize(Utils.HashToFileName(hash));

            // 检查是否支持断点续传
            var isRangeRequest = context.Request.Headers.ContainsKey("Range");
            if (isRangeRequest)
            {
                // 解析 Range 头部，获取断点续传的起始位置和结束位置
                var rangeHeader = context.Request.Headers["Range"].ToString();
                var (startByte, endByte) = GetRange(rangeHeader, fileSize);

                // 设置响应头部
                context.Response.StatusCode = 206; // Partial Content
                context.Response.Headers.Append("Accept-Ranges", "bytes");
                context.Response.Headers.Append("Content-Range", $"bytes {startByte}-{endByte}/{fileSize}");
                // context.Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
                context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(hash)}\"");

                // 计算要读取的字节数
                var totalBytesToRead = endByte - startByte + 1;

                using (Stream file = cluster.storage.ReadFileStream(Utils.HashToFileName(hash)))
                {
                    context.Response.Headers["Content-Length"] = totalBytesToRead.ToString();

                    file.Seek(startByte, SeekOrigin.Begin);
                    byte[] buffer = new byte[4096];
                    for (; file.Position < endByte;)
                    {
                        int count = file.Read(buffer, 0, buffer.Length);
                        if (file.Position > endByte && file.Position - count < endByte) await context.Response.Body.WriteAsync(buffer[..(int)(count - file.Position + endByte + 1)]);
                        else if (count != buffer.Length) await context.Response.Body.WriteAsync(buffer[..(count)]);
                        else await context.Response.Body.WriteAsync(buffer);
                    }
                }
                LogAccess(context);
                return new FileAccessInfo
                {
                    hits = 1,
                    bytes = totalBytesToRead
                };
            }
            else
            {
                // 设置响应头部
                context.Response.Headers.Append("Accept-Ranges", "bytes");
                context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(hash)}\"");
                //context.Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
                context.Response.Headers.Append("Content-Range", $"bytes {0}-{fileSize - 1}/{fileSize}");
                LogAccess(context);
                return await cluster.storage.HandleRequest(Utils.HashToFileName(hash), context);
            }
        }


        private static (long startByte, long endByte) GetRange(string rangeHeader, long fileSize)
        {
            if (rangeHeader.Length <= 6) return (0, fileSize);
            var ranges = rangeHeader[6..].Split("-");
            try
            {
                if (ranges[1].Length > 0)
                {
                    return (long.Parse(ranges[0]), long.Parse(ranges[1]));
                }
            }
            catch (Exception)
            {
                return (long.Parse(ranges[0]), fileSize - 1);
            }

            return (long.Parse(ranges[0]), fileSize - 1);
        }
    }
}
