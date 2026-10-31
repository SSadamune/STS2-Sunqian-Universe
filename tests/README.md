# 孙乾宇宙自动化战斗测试

本目录使用 [TestTheSpire](https://github.com/shadowversebydmod/TestTheSpire)
以 headless 模式启动 Slay the Spire 2，并在真实战斗中执行 xUnit 断言。

## 运行

`release/version-public` 面向游戏 **0.107.1**，使用 .NET 9 和
`STS2.RitsuLib.Compat.0.107.1`。正式版引用保存在 `lib/public/`，
测试版引用保存在 `lib/beta/`，均不受 Git 管理。当前 main 仍使用旧路径
`lib/sts2.dll`，该路径保留测试版引用；本分支全部项目只引用 `lib/public/`。
切换分支不需要覆盖引用。更新游戏后可执行
`./devtools/cache-game-references.ps1 -Channel public` 或 `-Channel beta`
刷新对应缓存（脚本会核对游戏版本，且不会覆盖另一套缓存）。

正式版测试使用仓库内的 `TestTheSpire.107` runner，并按清单里的实际 `id`
配置隔离测试存档，避免 `mod_manifest.json` 文件名导致外部模组漏禁用。

在仓库根目录执行：

```powershell
.\tests\run-tests.ps1
```

仅列出发现的测试，不执行用例：

```powershell
.\tests\run-tests.ps1 -ListOnly
```

按名称筛选测试：

```powershell
.\tests\run-tests.ps1 -Filter FlameStrike
```

加载 `DefenseAwareDebuffPreview` 的独立互操作测试模式：

```powershell
.\tests\run-tests.ps1 -DefenseAwareDebuffPreviewTests -Filter DefenseAwareDebuffPreview
```

该模式要求游戏的 `mods/DefenseAwareDebuffPreview` 中已安装 v0.1.1，
并通过 `Sts2RequiredModIds` 将它真正加入 TestTheSpire 载荷。普通模式不会加载它，
因此两种模式分别覆盖可选模组缺失时的基础色块和存在时的防御感知色块。

脚本会构建并安装当前模组，把完整的 Workshop RitsuLib 同步到游戏的
`mods/STS2-RitsuLib`，再由 TestTheSpire 安装 runner、xUnit 断言程序集和测试模组。
测试配置与存档位于 `SunqianUniverse.Tests/obj/Sts2TestData`，不会覆盖正常游戏配置。

Windows 下脚本会把 `APPDATA`、`LOCALAPPDATA` 和 `XDG_DATA_HOME` 同时重定向到
该测试目录。headless 进程默认超过 180 秒即终止，可通过 `-TimeoutSeconds` 调整。

使用 `-SkipModInstall` 可以复用当前已安装的孙乾宇宙 DLL 与 PCK。如果安装位置不同，
可通过 `-GameDir`、`-RitsuLibDir` 或 `-GodotExe` 覆盖脚本默认值。

## 当前覆盖

- `FlameStrikeTests`：烈焰打击的伤害与燃烧分支烟雾测试。
- `BowangParadoxTests`：BP-01 至 BP-06，覆盖混合手牌、单回合保留、脚本失效弃牌，
  以及普通/升级小黄门对新抽燃烧牌的保护顺序。
- `EunuchScriptTests`：EU-01、EU-02，覆盖新旧小黄门替换、各能力实例的口信牌归属隔离，
  以及口信牌离手后的标记清理与停止追踪。
- Common 卡牌遵循“一张牌一个 `*Tests.cs`”的结构，覆盖直接伤害、格挡、状态、
  牌堆、自动打出、蓄能、回收和剧本生成等行为。
- `CommonCardPoolTests`：从孙乾运行时卡池审计全部 20 张 Common 卡；新增 Common 卡却
  未创建测试并更新测试清单时，此审计会直接失败。

Common 覆盖清单：`Audition`、`ChickenFootCheeseScript`、`Circus`、`Denied`、
`EunuchScript`、`ExecutionCommanderScript`、`FingerSnapStrike`、`FireNova`、
`HateTheHeavensStrike`、`HeadsmanScript`、`LaserSwordAssassination`、`Portal`、
`SendHorseWithoutHay`、`ServantScript`、`StarryNightStrike`、`VoiceChange`、
`WatchFireFromShore`、`Wine`、`WorkLifeBalance`、`WorryWithoutWorry`。

测试类统一继承 `SunqianCombatTestSuite`。新增用例时优先使用其中的牌堆、升级、
单回合保留和脚本断言辅助方法，避免直接改写游戏内部牌堆。
