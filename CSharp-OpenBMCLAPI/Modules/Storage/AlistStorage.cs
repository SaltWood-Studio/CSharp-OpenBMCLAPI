using CSharpOpenBMCLAPI.Modules.WebServer;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public class AlistStorage : IStorage
    {
        protected HttpClient client;
        protected string baseAddr;
        protected StorageUser user;

        public AlistStorage()
        {
            this.client = new HttpClient();
            this.baseAddr = "BMCLAPI/cache";
            this.client.BaseAddress = new Uri(ClusterRequiredData.Config.clusterFileDirectory);
            this.user = ClusterRequiredData.Config.storageUser;
        }

        public bool Exists(string hashPath)
        {
            string path = GetAbsolutePath(hashPath);
            HttpContent content = JsonContent.Create(new
            {
                path = path,
                password = ""
            });
            AlistReturnResult? result = this.client.PostAsync("api/fs/get", content).Result.Content.ReadFromJsonAsync<AlistReturnResult>().Result;
            if (result != null)
            {
                return result.code >= 200 && result.code < 300;
            }
            return false;
        }

        public void GarbageCollect(IEnumerable<ApiFileInfo> files)
        {
            throw new NotImplementedException();
        }

        public string GetAbsolutePath(string path)
        {
            return Path.Combine(baseAddr, path);
        }

        public long GetFileSize(string hashPath)
        {
            string path = GetAbsolutePath(hashPath);
            HttpContent content = JsonContent.Create(new
            {
                path = path,
                password = ""
            });
            AlistReturnResult? result = this.client.PostAsync("api/fs/get", content).Result.Content.ReadFromJsonAsync<AlistReturnResult>().Result;
            if (result != null)
            {
                if (result.data.TryGetValue("size", out object? size))
                {
                    return long.Parse(size.ToString() ?? "0");
                }
            }
            return 0;
        }

        protected string GetDirectLink(string hashPath)
        {
            string path = GetAbsolutePath(hashPath);
            HttpContent content = JsonContent.Create(new
            {
                path = path,
                password = ""
            });
            AlistReturnResult? result = this.client.PostAsync("api/fs/get", content).Result.Content.ReadFromJsonAsync<AlistReturnResult>().Result;
            if (result != null)
            {
                if (result.data.TryGetValue("raw_url", out object? link))
                {
                    return link as string ?? "";
                }
            }
            return "";
        }

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files)
        {
            throw new NotImplementedException();
        }

        public async Task<FileAccessInfo> HandleRequest(string hashPath, HttpContext context)
        {
            string url = GetAbsolutePath(hashPath);
            context.Response.StatusCode = 302;
            context.Response.Header["Location"] = url;
            await context.Response.WriteAsync(Array.Empty<byte>());
            return new FileAccessInfo
            {
                hits = 1,
                bytes = GetFileSize(hashPath)
            };
        }

        public void Initialize()
        {
            AlistReturnResult? result = JsonConvert.DeserializeObject<AlistReturnResult>(this.client.PostAsync("api/auth/login", JsonContent.Create(new
            {
                username = this.user.UserName,
                password = this.user.Password
            })).Result.Content.ReadAsStringAsync().Result);
            if (result != null)
            {
                if (result.data.TryGetValue("token", out object? token) && token as string != null)
                {
                    this.client.DefaultRequestHeaders.Add("Authorization", token as string);
                }
            }
        }

        public byte[] ReadFile(string hashPath)
        {
            string url = GetDirectLink(hashPath);
            HttpResponseMessage message = this.client.GetAsync(url).Result;
            return message.Content.ReadAsByteArrayAsync().Result;
        }

        public Stream ReadFileStream(string hashPath)
        {
            string url = GetDirectLink(hashPath);
            HttpResponseMessage message = this.client.GetAsync(url).Result;
            return message.Content.ReadAsStreamAsync().Result;
        }

        public void WriteFile(string hashPath, byte[] buffer)
        {
            HttpContent content = new ByteArrayContent(buffer);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, "api/fs/put");
            message.Content = content;
            message.Headers.Add("File-Path", hashPath);
            message.Headers.Add("Content-Length", buffer.Length.ToString());
            message.Headers.Add("Content-Type", "application/octet-stream");
            this.client.Send(message);
        }

        public void WriteFileStream(string hashPath, Stream stream)
        {
            HttpContent content = new StreamContent(stream);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, "api/fs/put");
            message.Content = content;
            message.Headers.Add("File-Path", hashPath);
            message.Headers.Add("Content-Length", stream.Length.ToString());
            message.Headers.Add("Content-Type", "application/octet-stream");
            this.client.Send(message);
        }
    }

    public class AlistReturnResult
    {
        public int code;
        public string message = string.Empty;
        public Dictionary<string, object> data = new();
    }
}
