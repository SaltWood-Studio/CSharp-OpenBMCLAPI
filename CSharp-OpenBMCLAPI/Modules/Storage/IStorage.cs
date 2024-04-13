using CSharpOpenBMCLAPI.Modules.WebServer;
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
    /// <summary>
    /// 存储接口，实现这个接口就能接入 Cluster
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// 初始化，可以在这里执行些类似登陆之类的操作
        /// </summary>
        public void Initialize();

        /// <summary>
        /// 写文件，将文件写入到存储
        /// </summary>
        /// <param name="hashPath"></param>
        /// <param name="buffer"></param>
        public void WriteFile(string hashPath, byte[] buffer);

        /// <summary>
        /// 读文件，将文件从存储读取并返回
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public byte[] ReadFile(string hashPath);

        /// <summary>
        /// 读文件，但是返回一个 Stream
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Stream ReadFileStream(string hashPath);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Exists(string hashPath);

        /// <summary>
        /// 获取绝对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetAbsolutePath(string path);

        /// <summary>
        /// 文件点名，查询缺失的文件
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public IEnumerable<ApiFileInfo> GetMissingFiles(IEnumerable<ApiFileInfo> files);

        /// <summary>
        /// GC，删除多余/过期的文件
        /// </summary>
        /// <param name="files"></param>
        public void GarbageCollect(IEnumerable<ApiFileInfo> files);

        /// <summary>
        /// 发送文件并计算统计数据
        /// </summary>
        /// <param name="hashPath"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task<FileAccessInfo> Express(string hashPath, HttpContext context);

        public long GetFileSize(string hashPath);
    }
}
