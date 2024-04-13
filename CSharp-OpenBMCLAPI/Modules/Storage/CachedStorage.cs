using CSharpOpenBMCLAPI.Modules.WebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;

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
        public int cleanupTime = 3 * 60;

        public CachedStorage(IStorage storage)
        {
            this.storage = storage;
        }

        public bool Exists(string hashPath)
        {
            return cache.ContainsKey(hashPath) || storage.Exists(hashPath);
        }

        public async Task<FileAccessInfo> Express(string hashPath, HttpContext context)
        {
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
            lock (cache)
            {
                GarbageCollectInternal();
            }
            storage.GarbageCollect(files);
        }

        private void GarbageCollectInternal()
        {
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
            return storage.GetAbsolutePath(path);
        }

        public long GetFileSize(string hashPath)
        {
            if (cache.ContainsKey(hashPath)) return cache[hashPath].content.LongLength;
            return storage.GetFileSize(hashPath);
        }

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files)
        {
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
            storage.Initialize();
        }

        public byte[] ReadFile(string hashPath)
        {
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