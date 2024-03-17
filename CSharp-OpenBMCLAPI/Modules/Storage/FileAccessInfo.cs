using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
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
