using Newtonsoft.Json;
using System.ComponentModel;

namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// 配置文件类
    /// </summary>
    public class Config
    {
        // 集群启动时的文件检查模式
        // 0: None（不检查，不推荐）
        // 1: Exists（检查文件是否存在）
        // 2: SizeOnly（检查文件大小，推荐，默认）
        // 3: Hash（完整计算哈希，时间长，推荐不常重启或是分片节点使用）
        public FileVerificationMode startupCheckMode;

        // 跳过启动前检查
        // 这会导致无法发现文件错误，但是能够将内存占用压缩到约正常情况下的 30%！
        // 当此项启用时，"startupCheckMode"无效
        public bool skipStartupCheck;

        // 指示 token 应当在距离其失效前的多少毫秒进行刷新
        public int refreshTokenTime;
        // 指示应该将要服务的文件放在哪里（服务路径）
        public string clusterFileDirectory;
        // 指示节点端的版本，不应由用户更改
        [Browsable(false)]
        public string clusterVersion;
        // 用户访问时使用的 IP 或域名
        [JsonProperty("host")]
        public string HOST { get; set; }
        // 对外服务端口
        [JsonProperty("port")]
        public ushort PORT { get; set; }
        // 是否使用自定义域名
        public bool byoc;
        // 指示是否执行快速上线，若为 true 则每次都不执行
        public bool noFastEnable;

        public bool disableAccessLog;

        public string cacheDirectory { get => Path.Combine(this.clusterFileDirectory, "cache"); }

        public Config()
        {
            this.startupCheckMode = FileVerificationMode.SizeOnly;
            this.skipStartupCheck = false;

            this.refreshTokenTime = 1800000;
            this.clusterFileDirectory = "./";
            this.clusterVersion = "1.9.8";

            this.HOST = "";
            this.PORT = 4000;
            this.byoc = false;
            this.noFastEnable = false;

            this.disableAccessLog = false;
        }
    }
}
