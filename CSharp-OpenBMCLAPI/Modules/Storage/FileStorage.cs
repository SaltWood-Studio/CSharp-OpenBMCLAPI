using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public class FileStorage : IStorage
    {
        protected string workingDirectory;
        public string CacheDirectory { get => Path.Combine(workingDirectory, "cache"); }

        public FileStorage(string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }

        public bool Exists(string path)
        {
            return File.Exists(Path.Combine(CacheDirectory, path));
        }

        public async Task<FileAccessInfo> Express(string hashPath, HttpContext context)
        {
            byte[] buffer = ReadFile(hashPath);
            await context.Response.BodyWriter.WriteAsync(buffer);
            return new FileAccessInfo()
            {
                hits = 1,
                bytes = buffer.LongLength
            };
        }

        public void GarbageCollect(IEnumerable<ApiFileInfo> files)
        {
            Queue<DirectoryInfo> queue = new Queue<DirectoryInfo>();
            queue.Enqueue(new DirectoryInfo(this.CacheDirectory));
            var fileHashes = files.Select(f => f.hash);

            while (queue.Count > 0)
            {
                DirectoryInfo directoryInfo = queue.Dequeue();

                foreach (DirectoryInfo info in  directoryInfo.EnumerateDirectories())
                {
                    queue.Enqueue(info);
                }

                foreach (FileInfo info in directoryInfo.EnumerateFiles())
                {
                    if (!fileHashes.Contains(info.Name))
                    {
                        SharedData.Logger.LogInfo($"删除无用文件：{info.Name}");
                        info.Delete();
                    }
                }
            }
        }

        public string GetAbsolutePath(string path)
        {
            return Path.Combine(this.CacheDirectory, path);
        }

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files)
        {
            List<ApiFileInfo> result = new List<ApiFileInfo>();
            foreach (ApiFileInfo file in files)
            {
                if (!File.Exists(Path.Combine(this.CacheDirectory, file.path)))
                {
                    result.Add(file);
                }
            }
            return result;
        }

        public void Initialize()
        {
            SharedData.Logger.LogInfo($"存储池类型 <{typeof(FileStorage).FullName}> 初始化完毕！");
        }

        public void WriteFile(string path, byte[] buffer)
        {
            using var file = File.Create(Path.Combine(this.CacheDirectory, path));
            file.Write(buffer);
        }

        public byte[] ReadFile(string path)
        {
            var file = File.ReadAllBytes(GetAbsolutePath(path));
            return file;
        }

        public Stream ReadFileStream(string path)
        {
            var file = File.OpenRead(GetAbsolutePath(path));
            return file;
        }

        public async Task<FileAccessInfo> Express(string hashPath, HttpContext context, (int from, int to) range)
        {
            using Stream stream = ReadFileStream(hashPath);
            stream.Position = range.from;

            byte[] buffer = new byte[range.to - range.from + 1];
            stream.Read(buffer);

            await context.Response.BodyWriter.WriteAsync(buffer);
            context.Response.BodyWriter.Complete();
            return new FileAccessInfo()
            {
                hits = 1,
                bytes = buffer.LongLength
            };
        }
    }
}
