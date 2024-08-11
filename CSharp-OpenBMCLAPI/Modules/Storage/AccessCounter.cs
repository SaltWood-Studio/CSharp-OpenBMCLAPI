namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 负责统计访问数据
    /// </summary>
    public struct AccessCounter
    {
        private object objectLock;

        public long Hits { get; private set; }
        public long Bytes { get; private set; }

        public AccessCounter()
        {
            this.objectLock = new();
        }

        /// <summary>
        /// 添加访问数据，在处理访问数据时调用
        /// </summary>
        /// <param name="fai"></param>
        public void Add(FileAccessInfo fai)
        {
            lock (this.objectLock)
            {
                Hits += fai.Hits;
                Bytes += fai.Bytes;
            }
        }

        /// <summary>
        /// 重置访问数据，在 KeepAlive 之后调用
        /// </summary>
        public void Reset()
        {
            Hits = 0;
            Bytes = 0;
        }
    }
}
