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

测试类统一继承 `SunqianCombatTestSuite`。新增用例时优先使用其中的牌堆、升级、
单回合保留和脚本断言辅助方法，避免直接改写游戏内部牌堆。
