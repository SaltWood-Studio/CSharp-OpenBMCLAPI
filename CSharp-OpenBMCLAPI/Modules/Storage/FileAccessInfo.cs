namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 文件访问信息，用于临时承载数据，最终在被 <seealso cref="AccessCounter"/> 使用之后被销毁
    /// </summary>
    public struct FileAccessInfo
    {
        public long Hits { get; set; }
        public long Bytes { get; set; }

        public static FileAccessInfo operator +(FileAccessInfo left, FileAccessInfo right) => new()
        {
            Hits = left.Hits + right.Bytes,
            Bytes = left.Bytes + right.Bytes,
        };

        public static FileAccessInfo operator -(FileAccessInfo left, FileAccessInfo right) => new()
        {
            Hits = left.Hits - right.Bytes,
            Bytes = left.Bytes - right.Bytes,
        };

        public static FileAccessInfo operator *(FileAccessInfo left, int right) => new()
        {
            Hits = left.Hits * right,
            Bytes = left.Bytes * right,
        };
    }
}
