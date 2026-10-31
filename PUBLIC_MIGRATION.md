# 正式版迁移记录

目标：游戏 **0.107.1**，模组 **0.3.1-public.1**。

## 0.3.1-public.1

在 `6d227ff`（0.2.19-public.1）之后获取远端 main，并合入 `4c9a44f`（#20）。
包含新版剧本收藏、激活与切换、激活上限、剧本升级生命周期、关帝形态重做、
盒饭交互、收工、素材复用及对应卡牌、资源和测试更新。

- 保留 .NET 9、public 引用缓存与正式版 RitsuLib 兼容包。
- 新增杂兵及其余攻击继续使用正式版 `FromCard(card)`；嵌套上将军按来源卡
  匹配当前资源窗口，保留跨玩家嵌套结算与资源继承。
- 正式版小黄门改接 `CardModel.OnTurnEndInHandWrapper`，保留灼伤等回合末效果，
  阻止其虚无消耗并还回手牌；旧口信离手清理已随 main 的新规则移除。
- 正式版没有独立的 `LocalSelector`，收藏的本地选择直接使用正式版选择器或
  本地 UI，不占用网络选择编号。
- 正式版在 `NCard.Reload` 内刷新肖像，剧本卡框在此后应用，不调用不存在的
  `UpdatePortrait`。
- 正式版新三国会在每张攻击牌打出前单独登记酒力。通过其已注册的酒力模型，
  阻止同持有者上将军子攻击重复登记消耗，确保刀斧手能正确恢复根牌消耗的酒力。
  适配只在新三国公开 API 就绪且战斗开始后安装，不引用外部 DLL。
- 接收 main 纳入版本管理的 `.uid` 与 `.import`；覆盖前的本地自动生成副本
  保存在 `.builds/pre-0.3.1-metadata/`。

## 0.2.19-public.1

合入 main 的 `f95d2b9`，包括 #16–#19 的全部内容：燃烧与一将成名联动、
大魏合唱团、原生家庭事件与相关遗物、卡牌重做、新三国联动和可选内容设置。
保留正式版分支已有的小黄门口信离手清理与兼职大将军复制牌修正。

### 0.2.19 阶段的 API 适配

参照[迁移教程](https://tutorials.sts2modding.com/docs/07-migration-99-100/)
的 0.107→0.108 及后续变更，反向适配并以本机正式版 DLL 验证：

- 使用 .NET 9、`STS2.RitsuLib.Compat.0.107.1`，游戏最低版本保持 0.107.1。
- 正式版没有 `CardPlay.Player` 和 `AttackCommand.CardPlay`：通过卡牌持有者、
  现有自动打出追踪与每张牌的嵌套 OnPlay 作用域保持资源继承和打击重定向行为。
- 保持正式版伤害钩子签名；燃烧使用正式版 `CreatureCmd.Damage` 的
  `dealer, cardSource` 参数顺序，保留来源卡且不引入伤害来源生物。
- 同时初始化小黄门清理与鸡脚奶酪系统；使用 main 的有序中毒/燃烧预览与可选预览 API，
  移除被替代的旧预测实现。
- 事件与回合结束测试使用正式版 API；测试框架按实际模组 ID 隔离可选依赖，
  并允许共享读取 Windows 子进程尚持有句柄的日志。

## 共用构建与测试流程

正式版三个项目统一引用 `lib/public/` 的 0.107.1 程序集；测试版 0.111.0
引用另存于 `lib/beta/`，并保留 main 使用的旧路径 `lib/sts2.dll` 为测试版副本。
两套缓存均不受 Git 管理，切换分支不会相互覆盖。需要更新引用时运行
`devtools/cache-game-references.ps1 -Channel public` 或 `-Channel beta`，
脚本只刷新与当前游戏版本匹配的一套引用。GodotSharp 也使用缓存，构建不依赖
此刻安装的是哪个游戏版本；运行测试仍须安装相应游戏。

```powershell
dotnet build sunqian-universe.sln
.\tests\run-tests.ps1 -TimeoutSeconds 600
.\tests\run-tests.ps1 -SkipModInstall -DefenseAwareDebuffPreviewTests -Filter DefenseAwareDebuffPreviewInteropTests
.\tests\run-joint-mod-tests.ps1 -SkipModInstall -TimeoutSeconds 600
```

联合测试前会核对外部模组的最低游戏版本，避免把 beta 专用依赖复制到正式版游戏。
测试配置和存档位于 `tests/SunqianUniverse.Tests/obj/Sts2TestData`，不改动玩家存档。

验证环境：游戏 0.107.1、RitsuLib 0.6.10、一将成名 1.2.5、
新三国 0.3.8-release.1、DefenseAwareDebuffPreview v0.1.2。
外部模组从 `steamapps/workshop/content/2868840` 的对应目录同步；
伤害预览模组使用本机已安装的正式版。

0.2.19-public.1 验证结果：构建 0 警告、0 错误；基础回归 294 项、
三国联动 46 项、伤害预览联动 7 项全部通过。

0.3.1-public.1 验证结果：构建 0 警告、0 错误；基础回归 566 项、
三国联动 46 项、伤害预览联动 9 项全部通过；最终卡框补丁另通过
8 项剧本交互专项复测，新增可选适配另通过 6 项缺失依赖回退测试。
联合测试按新版耗能与剧本词条更新准备数据，
保留原伤害、酒力继承、卡牌数值保留及悬停联动断言。
