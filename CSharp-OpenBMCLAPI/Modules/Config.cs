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
        // 指示应该将要服务的文件放在哪里（服务路径）
        public string clusterFileDirectory;
        public string clusterVersion;

        public Config()
        {
            this.refreshTokenTime = 1800000;
            this.clusterFileDirectory = "./";
            this.clusterVersion = "1.9.7";
        }
    }
}
