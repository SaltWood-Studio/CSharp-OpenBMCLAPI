using CSharpOpenBMCLAPI.Modules.WebServer;
using TeraIO.Network.WebDav;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public class WebDavStorage : IStorage
    {
        protected WebDavClient client;
        protected string baseAddr;
        private object writeLock;
        protected StorageUser user;

        public WebDavStorage()
        {
            this.client = new(ClusterRequiredData.Config.clusterFileDirectory);
            this.baseAddr = "BMCLAPI/cache";
            this.writeLock = new object();
            this.user = ClusterRequiredData.Config.storageUser;
        }

        public bool Exists(string hashPath)
        {
            return this.client.Exists(GetAbsolutePath(hashPath)).Result;
        }

        public void GarbageCollect(IEnumerable<ApiFileInfo> files)
        {
            var remoteFileHashes = this.client.ListFilesAndFolders(this.baseAddr).Result.Select(f => f.Split('/').Last());
            var fileHashes = files.Select(f => f.hash);
            foreach (var item in remoteFileHashes)
            {
                if (!fileHashes.Contains(item))
                {
                    this.client.Delete(item).Wait();
                    Logger.Instance.LogDebug($"Expired file {item} deleted.");
                }
            }
        }

        public string GetAbsolutePath(string path)
        {
            return Path.Combine(baseAddr, path);
        }

        public long GetFileSize(string hashPath)
        {
            return this.client.GetFileSize(GetAbsolutePath(hashPath)).Result;
        }

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files)
        {
            var remoteFileHashes = this.client.ListFilesAndFolders(this.baseAddr).Result.Select(f => f.Split('/').Last());
            foreach (var item in files)
            {
                if (!remoteFileHashes.Contains(item.hash))
                {
                    yield return item;
                }
            }
        }

        public async Task<FileAccessInfo> HandleRequest(string hashPath, HttpContext context)
        {
            string file = GetAbsolutePath(hashPath);
            context.Response.StatusCode = 302;
            context.Response.Header["Location"] = this.client.GetFileDownloadLink(file);
            await context.Response.WriteAsync(Array.Empty<byte>());
            return new FileAccessInfo
            {
                hits = 1,
                bytes = this.GetFileSize(file)
            };
        }

        public void Initialize()
        {
            this.client.SetUser(this.user.UserName, this.user.Password);
        }

        public byte[] ReadFile(string hashPath)
        {
            return this.client.GetFile(GetAbsolutePath(hashPath)).Result;
        }

        public Stream ReadFileStream(string hashPath)
        {
            return this.client.GetFileStream(GetAbsolutePath(hashPath)).Result;
        }

        public void WriteFile(string hashPath, byte[] buffer)
        {
            lock (writeLock) // 由于多线程会炸所以加一个互斥锁
            {
                this.client.Delete(GetAbsolutePath(hashPath)).Wait();
                this.client.Lock(GetAbsolutePath(hashPath)).Wait();
                this.client.PutFile(GetAbsolutePath(hashPath), buffer).Wait();
                this.client.Unlock(GetAbsolutePath(hashPath)).Wait();
            }
        }

        public void WriteFileStream(string hashPath, Stream stream)
        {
            lock (writeLock) // 由于多线程会炸所以加一个互斥锁
            {
                this.client.Delete(GetAbsolutePath(hashPath)).Wait();
                this.client.Lock(GetAbsolutePath(hashPath)).Wait();
                this.client.PutFile(GetAbsolutePath(hashPath), stream).Wait();
                this.client.Unlock(GetAbsolutePath(hashPath)).Wait();
            }
        }
    }
}
