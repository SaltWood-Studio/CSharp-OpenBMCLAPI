using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public interface ICachedStorage : IStorage
    {
        public abstract long GetCachedFiles();
        public abstract long GetCachedMemory();
    }
}
