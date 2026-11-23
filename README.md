# ChevalGrandSlay
ChevalGrandSlay 是《杀戮尖塔 2》的同人角色 Mod，加入《赛马娘 Pretty Derby》中的**高尚骏逸（Cheval Grand）**。她的核心玩法是管理每张牌的体力，在战斗之间取舍补充新牌和恢复旧牌。

## 玩法
- **体力与休整牌堆**：带有「体力」的牌通常每打出一次消耗 1 点体力，剩余体力跨战斗保留。体力耗尽的牌会进入休整牌堆，可通过卡牌、能力或战后奖励恢复。
- **战后奖励**：击败敌人后有机会获得一张新体力牌，或回满牌组中一张牌的体力。两项奖励同时出现时，只能领取其中一项。
- **领域**：打出「我也能耀眼」可进入领域，回收休整中的牌，并获得额外能量和攻击伤害。若倒计时结束时仍未赢下战斗，高尚骏逸会死亡。

Mod 包含专属卡牌、遗物、药水、角色动画和音效，提供简体中文及英文本地化。

## 安装（Windows Steam）
> Mod 版本为 0.1.0。声明最低游戏版本为 0.113.0，最低前置版本为 STS2-RitsuLib 0.6.13。
### Steam Workshop（推荐）
https://steamcommunity.com/sharedfiles/filedetails/?id=3840359479 

### 本地安装
1. 关闭游戏，将适配当前游戏版本的 RitsuLib 安装到游戏目录下的 mods/STS2-RitsuLib/。RitsuLib 0.6.13 发布页
2. 将本 Mod 的 ChevalGrandSlay.dll、`ChevalGrandSlay.pck` 和 ChevalGrandSlay.json 一起放入 mods/ChevalGrandSlay/。
3. 启动游戏，按提示允许加载 Mod，然后在角色选择界面选择高尚骏逸。

### 从源码构建
   >需要 Windows 版《杀戮尖塔 2》、.NET 10 SDK 和 Godot 4.5.1 .NET 版。

#### 首次配置: 
1. 根目录执行
~~~powershell
Copy-Item .\ChevalGrandSlay\local.props.template .\ChevalGrandSlay\local.props`
~~~
3. 在 `ChevalGrandSlay/local.props` 中填写 `Sts2Dir`（游戏安装目录）、`GodotExe`（Godot 可执行文件的完整路径）和 `AutoAnthonyDll`（东尼算法原版 DLL 的完整路径）。如果游戏程序集位于其他目录，一并调整 `Sts2DataDir`。
4. 根目录执行
~~~powershell
`dotnet build .\ChevalGrandSlay\ChevalGrandSlay.csproj -c Release`
~~~
默认构建会编译 DLL、导出 PCK，并将 Mod 部署到游戏目录下的 mods/ChevalGrandSlay/。

## Credits
- きなこ餅 https://www.pixiv.net/artworks/115904172 シュヴァルグラン rkgk (CharacterSelectBG)
- ちょくエジ #Pixiv https://www.pixiv.net/artworks/150734147 どあっぷシュヴァち (CharacterIcon)
