using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public interface IStorage
    {
        public void Initialize();

        public void WriteFile(string path, byte[] buffer);

        public byte[] ReadFile(string path);

        public Stream ReadFileStream(string path);

        public bool Exists(string path);

        public string GetAbsolutePath(string path);

        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files);

        public void GarbageCollect(IEnumerable<ApiFileInfo> files);

        public Task<FileAccessInfo> Express(string hashPath, HttpContext context);

        public Task<FileAccessInfo> Express(string hashPath, HttpContext context, (int from, int to) range);
    }
}
