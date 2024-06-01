namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 文件信息结构
    /// </summary>
    public struct ApiFileInfo
    {
        public string path;
        public string hash;
        public long size;
        public long mtime;
    }
}
