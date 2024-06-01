using CSharpOpenBMCLAPI.Modules.WebServer;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public struct CachedFile
    {
        private object locker;
        internal byte[] content;
        public long LastAccessTime { get; private set; }
        public byte[] Content
        {
            get
            {
                lock (locker)
                {
                    LastAccessTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                    return content;
                }
            }
        }

        public CachedFile(byte[] content)
        {
            this.content = content;
            locker = new object();
        }
    }

    public class CachedStorage : ICachedStorage
    {
        private Dictionary<string, CachedFile> cache = new();
        private IStorage storage;
        private Task? _cleanupTask;
        private Task? _watchdogTask;
        public int cleanupTime = 3 * 60;
        protected bool cacheEnabled = true;

        public CachedStorage(IStorage storage)
        {
            this.storage = storage;
            if (ClusterRequiredData.Config.maxCachedMemory == 0) this.cacheEnabled = false;
        }

        public bool Exists(string hashPath)
        {
            if (!cacheEnabled) return this.storage.Exists(hashPath);
            return cache.ContainsKey(hashPath) || storage.Exists(hashPath);
        }

        public async Task<FileAccessInfo> HandleRequest(string hashPath, HttpContext context)
        {
            if (!cacheEnabled) return await this.storage.HandleRequest(hashPath, context);
            if (!cache.ContainsKey(hashPath))
            {
                byte[] bytes = this.ReadFile(hashPath);
                await context.Response.Stream.WriteAsync(bytes);
                this[hashPath] = new CachedFile(bytes);
                return new FileAccessInfo
                {
                    hits = 1,
                    bytes = bytes.LongLength
                };
            }
            else
            {
                await context.Response.WriteAsync(this[hashPath].Content);
                return new FileAccessInfo
                {
                    hits = 1,
                    bytes = GetFileSize(hashPath)
                };
            }
        }

        public void GarbageCollect(IEnumerable<ApiFileInfo> files)
        {
            if (!cacheEnabled)
            {
                this.storage.GarbageCollect(files);
                return;
            }
            lock (cache)
            {
                GarbageCollectInternal();
            }
            storage.GarbageCollect(files);
        }

        private void GarbageCollectInternal()
        {
            if (!cacheEnabled) return;
            List<string> deleted = new();
            foreach (var file in cache)
            {
                if (file.Value.LastAccessTime + cleanupTime < DateTimeOffset.Now.ToUnixTimeSeconds())
                {
                    deleted.Add(file.Key);
                }
            }
            foreach (var file in deleted)
            {
                cache.Remove(file);
            }
        }

        public string GetAbsolutePath(string path)
        {
            if (!cacheEnabled) return this.storage.GetAbsolutePath(path);
            return storage.GetAbsolutePath(path);
        }

        public long GetFileSize(string hashPath)
        {
            if (!cacheEnabled) return this.storage.GetFileSize(hashPath);
            if (cache.ContainsKey(hashPath)) return cache[hashPath].content.LongLength;
            return storage.GetFileSize(hashPath);
        }

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files)
        {
            if (!cacheEnabled) return this.storage.GetMissingFiles(files);
            return storage.GetMissingFiles(files);
        }

        public void Initialize()
        {
            _cleanupTask = Task.Run(() =>
            {
                while (true)
                {
                    GarbageCollectInternal();
                    Thread.Sleep(60 * 1000);
                }
            });
            _watchdogTask = Task.Run(() =>
            {
                while (true)
                {
                    MemoryWatchdog();
                    Thread.Sleep(10 * 1000);
                }
            });
            storage.Initialize();
        }

        public void MemoryWatchdog()
        {
            double memory = GetCachedMemory();
            if (memory > 0 && memory > ClusterRequiredData.Config.maxCachedMemory * 1048576)
            {
                Logger.Instance.LogWarn($"缓存存储已大于 {ClusterRequiredData.Config.maxCachedMemory}（当前 {memory / 1048576}），已开始清理缓存");
                int count = 0;
                do
                {
                    string biggestFileKey = this.cache.OrderBy(kvp => kvp.Value.content.Length).Last().Key;
                    this.cache.Remove(biggestFileKey);
                    count++;
                }
                while (GetCachedMemory() > ClusterRequiredData.Config.maxCachedMemory || count <= 5); // 限制只有清理到内存低于指定值，并且清理次数大于五次才停止
            }
        }

        public byte[] ReadFile(string hashPath)
        {
            if (!cacheEnabled) return this.storage.ReadFile(hashPath);
            if (cache.ContainsKey(hashPath))
            {
                lock (cache)
                {
                    return this[hashPath].Content;
                }
            }
            else
            {
                byte[] bytes = storage.ReadFile(hashPath);
                cache.Add(hashPath, new CachedFile(bytes));
                return bytes;
            }
        }

        public Stream ReadFileStream(string hashPath)
        {
            if (!cacheEnabled) return this.storage.ReadFileStream(hashPath);
            if (cache.ContainsKey(hashPath))
            {
                lock (cache)
                {
                    return new MemoryStream(this[hashPath].Content);
                }
            }
            return storage.ReadFileStream(hashPath);
        }

        public void WriteFile(string hashPath, byte[] buffer)
        {
            if (!cacheEnabled) this.storage.WriteFile(hashPath, buffer);
            if (cache.ContainsKey(hashPath))
            {
                lock (cache)
                {
                    CachedFile temp = this[hashPath];
                    temp.content = buffer;
                    this[hashPath] = temp;
                }
                return;
            }
            storage.WriteFile(hashPath, buffer);
        }

        public void WriteFileStream(string hashPath, Stream stream)
        {
            if (!cacheEnabled) this.storage.WriteFileStream(hashPath, stream);
            if (cache.ContainsKey(hashPath))
            {
                lock (cache)
                {
                    CachedFile temp = this[hashPath];
                    temp.content = new byte[stream.Length];
                    stream.Read(temp.content, 0, temp.content.Length);
                    this[hashPath] = temp;
                }
                return;
            }
            storage.WriteFileStream(hashPath, stream);
        }

        public long GetCachedMemory()
        {
            return cache.Sum(c => c.Value.content.LongLength);
        }

        public long GetCachedFiles()
        {
            return cache.Count;
        }

        public CachedFile this[string key]
        {
            get => cache[key];
            set => cache[key] = value;
        }
    }
}