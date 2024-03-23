using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 文件信息结构
    /// </summary>
    public struct ApiFileInfo
    {
        public string path;
        public string hash;
        public int size;
    }
}
