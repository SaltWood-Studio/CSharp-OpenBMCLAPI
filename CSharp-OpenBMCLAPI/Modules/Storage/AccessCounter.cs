using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public class AccessCounter
    {
        public long hits;
        public long bytes;

        public AccessCounter() { }

        public void Add(FileAccessInfo fai)
        {
            hits += fai.hits;
            bytes += fai.bytes;
        }

        public void Reset()
        {
            hits = 0;
            bytes = 0;
        }
    }
}
