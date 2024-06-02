<div align="center">

![CSharp-OpenBMCLAPI](https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/assets/105980161/20b4b593-4ab9-445b-8a4b-6e329aa041c2)

# CSharp-OpenBMCLAPI
🎉 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的 C# 版本实现 🎉

  <a href="https://raw.githubusercontent.com/Mrs4s/go-cqhttp/master/LICENSE">
    <img src="https://img.shields.io/github/license/SALTWOOD/CSharp-OpenBMCLAPI" alt="license">
  </a>
  <a href="https://github.com/Mrs4s/go-cqhttp/releases">
    <img src="https://img.shields.io/github/v/release/SALTWOOD/CSharp-OpenBMCLAPI?color=blueviolet&include_prereleases" alt="release">
  </a>
  <a href="https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/actions">
    <img src="https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/workflows/.NET/badge.svg" alt="action">
  </a>
</div>

> 注：本仓库的代码参照 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的思路编写，部分特性、Bug 可能部分、甚至完全与 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 撞车。

> [!TIP]
> 由于作者学业繁忙，且~~懒得编译发布~~，所以 Release 构建会很不及时，有需要的人请自行构建源代码

# 项目说明

本项目是 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的 C# 版本，OpenBMCLAPI 是通过分布式集群帮助 [BMCLAPI](https://bmclapidoc.bangbang93.com/) 进行文件分发、加速中国大陆 Minecraft 下载的公益项目。

如果你想加入 OpenBMCLAPI，可以向 [bangbang93](https://github.com/bangbang93) 索取 `CLUSTER_ID` 和 `CLUSTER_SECRET`

# 贡献说明

如果你想为本项目做出贡献，请遵守以下规则：
1. 不要贡献比我的代码还烂的屎山
2. 每条 commit 请认真填写信息，可以加上一些前缀方便识别，请**不要使用类似“🐛修复漏洞”之类的无意义信息！**
3. 在提交 issue 之前，请认真确认**没有重复的 issue 再提交！**
4. 自己的代码，自己先测试，别一堆 bug 没修就想丢上来（尤其是想丢到 master 分支的，你丢 dev 分支还好说，只要不是编译都没法通过的那种），真的想请开 draft pull request
5. 请统一贡献到 **dev** 分支，master 分支**不接受** pull request，collaborator 们也**不要直接向 master 分支 commit！**

# 使用

详见 [Wiki](https://github.com/SALTWOOD/CSharp-OpenBMCLAPI/wiki)

# TODO

- [ ] ~~**[优先级高]** 仪表盘~~（等待第三方）
- [ ] **[优先级高]** 统一 API

**(以下暂缓进行)**

- [ ] 继续优化插件功能
- [ ] *[优先级低]* 高级控制台，在控制台中调整设置、查看监控信息而不只是看日志
___
- [x] ~~Docker 版本支持~~
- [x] ~~WebDAV 存储支持~~
- [x] ~~弃用 ASP.NET，使用其他框架~~
- [x] ~~插件功能，支持通过第三方插件做各种事情~~
- [x] ~~内存优化，使得程序可用~~
- [x] ~~上线节点，能够被主控登记为在线~~
- [x] ~~提供服务~~
- [ ] ~~Actions 自动构建~~

# ❓ FAQ
## 关于版本号
示例：v**1.9.7**-**1**
|  | 1.9.7 | 1 |
|--|-------|---|
| 名称 | **兼容版本号** | **fix 版本号** |
| 说明 | 表示该版本等效于 [OpenBMCLAPI](https://github.com/bangbang93/openbmclapi) 的哪个版本 | 表示在这个兼容版本号下第几个 Release，通常发生在 OpenBMCLAPI 没有更新但本仓库发现有 bug 并发布 Release 的情况下 |

# 鸣谢

**Newtonsoft**
- [Newtonsoft.Json](https://www.newtonsoft.com/json) - 使用 Newtonsoft.Json 解析 Json
- [Newtonsoft.Json.Bson](https://www.newtonsoft.com/json) - 使用 Newtonsoft.Json.Bson 解析 Bson，用于统计数据的存储

**@doghappy**
- [SocketIOClient](https://github.com/doghappy/socket.io-client-csharp) - 使用 SocketIOClient 与主控通信

**Apache**
- [log4net](https://logging.apache.org/log4net) - 使用 log4net 记录日志（虽然目前没用到）

**@oleg-st (Oleg Stepanischev)**
- [ZstdSharp.Port](https://github.com/oleg-st/ZstdSharp) - 使用 ZstdSharp.Port 解压主控下发的压缩过的文件列表

**SALTWOOD**
- [TeraIO](https://github.com/SALTWOOD/TeraIO) - 其实也没用到多少（目前是废物一个），以后会抽空把一些 utils 丢进去的

**@aaubry (Antoine Aubry)**
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - 用于配置文件的读写

**@2b2ttianxiu**
- 帮忙写了 **SimpleWebServer**，代替了 ASP.NET 提供文件分发

**@Mpdreamz (Martijn Laarman)**
- [shellprogressbar](https://github.com/Mpdreamz/shellprogressbar) 使用 shell progress bar 显示进度条

# 友情链接

[OpenBMCLAPI](https://github.com/bangbang93/openbmclapi)

[go-openbmclapi](https://github.com/LiterMC/go-openbmclapi)

[python-openbmclapi](https://github.com/TTB-Network/python-openbmclapi)
