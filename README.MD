<center>

![CSharp-OpenBMCLAPI](https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/assets/105980161/20b4b593-4ab9-445b-8a4b-6e329aa041c2)

# CSharp-OpenBMCLAPI
🎉 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的 C# 版本实现 🎉

</center>

<p align="center">
  <a href="https://raw.githubusercontent.com/Mrs4s/go-cqhttp/master/LICENSE">
    <img src="https://img.shields.io/github/license/SALTWOOD/CSharp-OpenBMCLAPI" alt="license">
  </a>
  <a href="https://github.com/Mrs4s/go-cqhttp/releases">
    <img src="https://img.shields.io/github/v/release/SALTWOOD/CSharp-OpenBMCLAPI?color=blueviolet&include_prereleases" alt="release">
  </a>
  <a href="https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/actions">
    <img src="https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/workflows/.NET/badge.svg" alt="action">
  </a>
</p>

> 注：本仓库的代码参照 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的思路编写，部分特性、Bug 可能部分、甚至完全与 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 撞车。

# 项目说明

本项目是 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的 C# 版本，OpenBMCLAPI 是通过分布式集群帮助 [BMCLAPI](https://bmclapidoc.bangbang93.com/) 进行文件分发、加速中国大陆 Minecraft 下载的公益项目。

如果你想加入 OpenBMCLAPI，可以寻找 [bangbang93](https://github.com/bangbang93) 索取 `CLUSTER_ID` 和 `CLUSTER_SECRET`

# 特性

- 能够正常跑起来
- 低内存占用（约 150MB）
- 支持多实例运行，适合带宽大于限制的富哥（

# 使用

## 从源码启动

1. 克隆仓库
2. 在 `CSharp-OpenBMCLAPI/CSharp-OpenBMCLAPI` 下新建 `.env.json`，按照如下方式填入 `ClusterId` 和 `ClusterSecret`：
```
{
	"ClusterId": "************************",
	"ClusterSecret": "********************************"
}
```
3. 运行 `dotnet build` 或打开项目，运行构建好的程序

## 从 Release 启动

1. 下载 Release 版本，解压缩
2. 在工作目录下新建 `.env.json`，填入 `ClusterId` 和 `ClusterSecret`（格式参见“从源码启动”）
3. 双击可执行文件，启动集群

# TODO

- [ ] 原生多实例运行
- [ ] 插件功能，支持通过第三方插件做各种事情
- [ ] *[优先级低]* 高级控制台，在控制台中调整设置、查看监控信息而不只是看日志
- [x] 内存优化，使得程序可用
- [x] 上线节点，能够被主控登记为在线
- [x] 提供服务

# 已知问题
- 目前对其他存储方式的兼容不完善，文件方式存储根深蒂固了，要时间改善
- ~~当一次性下载文件太多时，有概率报 **NullReferenceException**~~

# 碎碎念
## 关于版本号
示例：v**1.9.7**-**1**
|  | 1.9.7 | 1 |
|--|-------|---|
| 名称 | **兼容版本号** | **fix 版本号** | **本体版本号** |
| 说明 | 表示该版本等效于 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的哪个版本 | 表示在这个兼容版本号下第几个 Release，通常发生在 OpenBMCLAPI 没有更新但本仓库发现有 bug 并发布 Release 的情况下 |

## 你知道吗

- 本仓库只经历了 3 个 commit，就实现了内存占用从 2GB 到 150MB 的突破，瞬间从劣势变成优势！

# 鸣谢

**Newtonsoft**
- [Newtonsoft.Json](https://www.newtonsoft.com/json) - 使用 Newtonsoft.Json 解析 Json

**Behzad Khosravifar**
- [Downloader](https://github.com/bezzad/Downloader) - 使用 Downloader 下载文件

**@doghappy**
- [SocketIOCLient](https://github.com/doghappy/socket.io-client-csharp) - 使用 SocketIOClient 与主控通信

**Apache**
- [Avro](https://avro.apache.org/) - 使用 Avro 包解析主控返回的解压缩过的文件列表
- [log4net](https://logging.apache.org/log4net/) - 使用 log4net 记录日志（虽然目前没用到）

**@oleg-st (Oleg Stepanischev)**
- [ZstdSharp.Port](https://github.com/oleg-st/ZstdSharp) - 使用 ZstdSharp.Port 解压主控下发的压缩过的文件列表

**SALTWOOD**
- [TeraIO](https://github.com/SALTWOOD/TeraIO) - 其实也没用到多少（目前是废物一个），以后会抽空把一些 utils 丢进去的
