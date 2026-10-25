# 孙乾宇宙自动化战斗测试

本目录使用 [TestTheSpire](https://github.com/shadowversebydmod/TestTheSpire)
以 headless 模式启动 Slay the Spire 2，并在真实战斗中执行 xUnit 断言。

## 运行

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
- `BowangParadoxTests`：覆盖混合手牌、单回合保留、二选一激活与剧本失效行为。
- `EunuchScriptTests`：EU-01 至 EU-06，覆盖打出时赋予临时虚无、打出后移除、
  激活期间将所有手牌虚无视为保留、抑制虚无消耗、触发保留钩子、博望悖论交互，
  以及《灼伤》一类具有独立回合末效果的牌。
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
