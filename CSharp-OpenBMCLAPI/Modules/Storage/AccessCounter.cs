using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 负责统计访问数据
    /// </summary>
    public class AccessCounter
    {
        public long hits;
        public long bytes;

        public AccessCounter() { }

        /// <summary>
        /// 添加访问数据，在处理访问数据时调用
        /// </summary>
        /// <param name="fai"></param>
        public void Add(FileAccessInfo fai)
        {
            lock (this)
            {
                hits += fai.hits;
                bytes += fai.bytes;
            }
        }

        /// <summary>
        /// 重置访问数据，在 KeepAlive 之后调用
        /// </summary>
        public void Reset()
        {
            hits = 0;
            bytes = 0;
        }
    }
}
