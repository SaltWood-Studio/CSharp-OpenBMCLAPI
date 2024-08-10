using CSharpOpenBMCLAPI.Modules.Storage;
using YamlDotNet.Serialization;

namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// 配置文件类
    /// </summary>
    public class Config
    {
        [YamlMember(Description = """
        0: None（不检查，不推荐）")]
        1: Exists（检查文件是否存在）")]
        2: SizeOnly（检查文件大小，推荐，默认）")]
        3: Hash（完整计算哈希，时间长，推荐不常重启或是分片节点使用）
        """, Order = 2)]
        public FileVerificationMode startupCheckMode;

        [YamlMember(Description = """
        跳过检查
        这会导致无法发现文件错误，但是能够将内存占用压缩到很低！
        [注] 当此项启用时，"startupCheckMode"无效
        """, Order = 2)]
        public bool skipCheck;

        [YamlMember(Description = "指示 token 应当在距离其失效前的多少毫秒进行刷新", Order = 1)]
        public int refreshTokenTime;

        [YamlMember(Description = "指示应该将要服务的文件放在哪里（服务路径）", Order = 1)]
        public string clusterFileDirectory;

        [YamlMember(Description = "指示节点的工作路径", Order = 1)]
        public string clusterWorkingDirectory;

        [YamlMember(Description = """
            节点存储模式，可用值：
            - File，此时 clusterFileDirectory 为存储位置
            - WebDav，此时 StorageUser 指示验证用户名和密码，clusterFileDirectory 填写 WebDav 地址
            - Alist，此时 StorageUser 指示验证用户名和密码，clusterFileDirectory 填写 Alist 地址
            """, Order = 1)]
        public StorageType StorageType { get; set; }

        [YamlIgnore]
        [YamlMember(Description = "指示节点端的版本，不应由用户更改")]
        public string clusterVersion;

        [YamlMember(Alias = "host", Description = "用户访问时使用的 IP 或域名", Order = 0)]
        public string HOST { get; set; }

        [YamlMember(Alias = "port", Description = "对外服务端口", Order = 0)]
        public ushort PORT { get; set; }

        [YamlMember(Alias = "bringYourOwnCertificate", Description = "是否不使用主控分发的证书", Order = 1)]
        public bool BringYourOwnCertficate { get; set; }


        [YamlMember(Description = "[开发变量]\n指示是否不执行快速上线，若为 true 则每次都不执行", Order = 10)]
        public bool NoFastEnable { get; set; }


        [YamlMember(Description = "指示是否禁用访问日志输出", Order = 1)]
        public bool DisableAccessLog { get; set; }

        [YamlMember(Description = "[开发变量]\n指示是否进行节点上线", Order = 10)]
        public bool NoEnable { get; set; }

        [YamlMember(Description = "指示缓存存储最大占用内存（MB 为单位）\n-1 为不限制，0 为禁用缓存存储", Order = 1)]
        public double maxCachedMemory;

        [YamlIgnore]
        public string cacheDirectory { get => Path.Combine(this.clusterWorkingDirectory, "cache"); }

        [YamlMember(Description = "[开发变量]\n主控地址", Order = 10)]
        public string CenterServerAddress { get; internal set; }

        [YamlMember(Description = "指示登录存储池需要的凭据，如果存储池不需要则可以忽略", Order = 1)]
        public StorageUser storageUser;

        [YamlMember(Description = "指示从主控下载文件的线程数，设置为 0 则使用主控要求的线程数")]
        public int DownloadFileThreads { get; internal set; }

        [YamlMember(Description = "[开发变量]\n关闭签名校验", Order = 10)]
        public bool NoSignatureVerifying { get; internal set; }

        [YamlMember(Description = "[开发变量]\n关闭证书（HTTP）", Order = 10)]
        public bool NoCertificate { get; internal set; }

        public Config()
        {
            this.startupCheckMode = FileVerificationMode.SizeOnly;
            this.skipCheck = false;

            this.refreshTokenTime = 1800000;
            this.clusterWorkingDirectory = "./working";
            this.clusterFileDirectory = "./cache";
            this.clusterVersion = "1.11.0";

            this.HOST = "";
            this.PORT = 4000;
            this.BringYourOwnCertficate = false;
            this.NoFastEnable = false;

            this.DisableAccessLog = false;
            this.NoEnable = false;
            this.storageUser = new StorageUser();
            this.maxCachedMemory = 1024;
            this.DownloadFileThreads = 0;
            this.CenterServerAddress = "https://openbmclapi.bangbang93.com/";
            this.NoSignatureVerifying = false;
            this.NoCertificate = false;

            this.StorageType = StorageType.File;
        }
    }
}
