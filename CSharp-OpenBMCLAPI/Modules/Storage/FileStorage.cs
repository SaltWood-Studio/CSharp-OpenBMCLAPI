using CSharpOpenBMCLAPI.Modules.WebServer;
using System.Text.RegularExpressions;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 文件存储类，实现 <seealso cref="IStorage"/>
    /// </summary>
    public class FileStorage : IStorage
    {
        protected string workingDirectory;
        public string CacheDirectory { get => workingDirectory; }
        private SambaConnection? connection;

        public FileStorage(string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
            if (workingDirectory.StartsWith(@"\\"))
            {
                StorageUser user = ClusterRequiredData.Config.storageUser;
                this.connection = new SambaConnection(user.UserName, user.Password, Regex.Match(workingDirectory, @"\\\\(.*?)\\").Groups[1].Value);
            }
        }

        public bool Exists(string hashPath)
        {
            return File.Exists(GetAbsolutePath(hashPath));
        }

        public void GarbageCollect(IEnumerable<ApiFileInfo> files)
        {
            Queue<DirectoryInfo> queue = new Queue<DirectoryInfo>();
            queue.Enqueue(new DirectoryInfo(this.CacheDirectory));
            var fileHashes = files.Select(f => f.hash);

            while (queue.Count > 0)
            {
                DirectoryInfo directoryInfo = queue.Dequeue();

                foreach (DirectoryInfo info in directoryInfo.EnumerateDirectories())
                {
                    queue.Enqueue(info);
                }

                foreach (FileInfo info in directoryInfo.EnumerateFiles())
                {
                    if (!fileHashes.Contains(info.Name))
                    {
                        Logger.Instance.LogInfo($"删除无用文件：{info.Name}");
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
                if (!File.Exists(GetAbsolutePath(Utils.HashToFileName(file.hash))))
                {
                    result.Add(file);
                }
            }
            return result;
        }

        public void Initialize()
        {
            Logger.Instance.LogInfo($"存储池类型 <{typeof(FileStorage).FullName}> 初始化完毕！");
        }

        public void WriteFile(string hashPath, byte[] buffer)
        {
            Directory.CreateDirectory(GetAbsolutePath(Path.GetDirectoryName(hashPath).ThrowIfNull()));
            using var file = File.Create(GetAbsolutePath(hashPath));
            file.Write(buffer);
        }

        public void WriteFileStream(string hashPath, Stream stream)
        {
            Directory.CreateDirectory(GetAbsolutePath(Path.GetDirectoryName(hashPath).ThrowIfNull()));
            using var file = File.Create(GetAbsolutePath(hashPath));
            stream.CopyTo(file);
        }

        public byte[] ReadFile(string hashPath)
        {
            var file = File.ReadAllBytes(GetAbsolutePath(hashPath));
            return file;
        }

        public Stream ReadFileStream(string hashPath)
        {
            var file = File.OpenRead(GetAbsolutePath(hashPath));
            return file;
        }

        public async Task<FileAccessInfo> HandleRequest(string hashPath, HttpContext context)
        {
            string filePath = GetAbsolutePath(hashPath);
            await context.Response.SendFile(filePath);

            FileInfo fileInfo = new FileInfo(filePath);

            return new FileAccessInfo()
            {
                hits = 1,
                bytes = fileInfo.Length
            };
        }

        public long GetFileSize(string hashPath)
        {
            FileInfo info = new FileInfo(GetAbsolutePath(hashPath));
            return info.Length;
        }
    }
}
