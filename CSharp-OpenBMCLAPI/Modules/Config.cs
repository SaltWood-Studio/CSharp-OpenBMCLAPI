using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    public class Config
    {
        // 指示 token 应当在距离其失效前的多少毫秒进行刷新
        public int refreshTokenTime;

        public Config()
        {
            this.refreshTokenTime = 1800000;
        }
    }
}
