namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 文件访问信息，用于临时承载数据，最终在被 <seealso cref="AccessCounter"/> 使用之后被销毁
    /// </summary>
    public struct FileAccessInfo
    {
        public long hits;
        public long bytes;

        public static FileAccessInfo operator +(FileAccessInfo left, FileAccessInfo right) => new()
        {
            hits = left.hits + right.bytes,
            bytes = left.bytes + right.bytes,
        };

        public static FileAccessInfo operator -(FileAccessInfo left, FileAccessInfo right) => new()
        {
            hits = left.hits - right.bytes,
            bytes = left.bytes - right.bytes,
        };

        public static FileAccessInfo operator *(FileAccessInfo left, int right) => new()
        {
            hits = left.hits * right,
            bytes = left.bytes * right,
        };
    }
}
