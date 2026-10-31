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

- `ScriptCollectionButtonGlowTests`：覆盖紫色外发光随剧本记录、取消选择、激活次数、能量与休眠恢复刷新，
  并验证发光层不截获按钮输入。
- `PortalTests`：覆盖自动打出的普通与升级攻击类剧本均加入剧本集并激活、替换旧剧本，
  以及抽到《隔岸观火》时留在手牌（包括打出传送门前手牌已满的情况）。
- `KillingIntentPerceptionMultiplayerTests`：覆盖《吃什么》后双方嵌套《杀意感知》的选牌与自动打出，
  跨玩家选择保持在同次结算内，结算结束后双方中央出牌区清空；验证A的5活力与B的3活力
  叠加传给第二张《杀意感知》及后续攻击，且嵌套加成不会影响外层的后续牌或结算后的手动打出。
  还覆盖《杀意感知》打出盟友普通/升级《闪电战》的递归打击链，以及《兼职将军》生成的每张攻击牌。
- `SunqianUniverseTests`：覆盖升级获得固有、固定2敏捷、每次剧本失效后抽2张、能力叠加、
  直接加入与返回剧本集，以及素材复用强化、重复加入和排序不抽牌。
- `DistributedScreenwriterTests`：覆盖普通变化、升级后生成强化剧本且不额外抽牌，以及混合叠加。
- `BurningLinkedCampsBonusTests`：覆盖灼伤使已有好火啊增加20个百分点、隐藏加成累积并由下一张
  《上方谷之叹》消耗、手牌和打出中的绿色预览，以及隐藏加成在剧本失效后保留。
- `XiliangSavageScriptTests`：覆盖打出时获得5/8活力与16/21格挡、重新激活不获得活力，
  以及活力数值保留与多段伤害交互。
- `OneManOneCityTests`：覆盖激活时立即触发覆甲并消耗一层、盒饭重复激活、最后一层覆甲，
  以及移除原持续双倍格挡和额外衰减效果。
- `GuanDiFormTests`：覆盖文、武关羽加入剧本集后立即重选激活、激活上限增加与互斥，
  文关羽获得敏捷直到失效、回合开始失去格挡时对所有敌人造成等量伤害，武关羽获得力量直到失效并按攻击牌消耗的能量获得等量格挡，以及新回合不自动切换。
- `ScriptInteractionTests`：覆盖关羽互斥优先于先入先出、取消后重新点选的顺序、确认页面返回顺序、激活效果与能力挂载及批量失效顺序、专用卷轴卡框和普通牌节点复用。
- `TextSynchronizationTests`：直接检查中英文资源中的展示牌名称、激活与蓄能文案、
  《千变万化》提示和《长厚似伪》动态数值；验证基础与升级后的卡牌、能力描述一致。
- `ScriptSystemTests`：覆盖剧本集悬停说明、重选提示中的剩余次数、盒饭的2回合休眠与倒计时、
  丰盛盒饭每回合一次付费激活及回合开始获得能量，
  本地确认或取消重选不消耗联机选牌编号，以及之后打出《收工》的结算，
  以及无遗物、次数耗尽和能量不足时的只读界面下方文本；重选界面同时检查提示文本、
  选满后的轮换和返回按钮取消行为。
- `FlameStrikeTests`：烈焰打击的伤害与燃烧分支烟雾测试。
- `BowangParadoxTests`：覆盖“进军”和“放火”的独立记录与激活、素材复用升级两个分支、
  两种记录计入激活上限门槛、盒饭直接激活、混合手牌、单回合保留与剧本失效行为。
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

- 剧本升级扩展：覆盖激活文案的剧本+与绿色升级预览、二言诗人混合升级顺序及累计活力/回合触发、夜袭乌巢素材复用升级次数与存档往返。

- 素材复用扩展：行刑指挥官 50%+每级25%、博望两分支 7+每级2、刀斧手 3+每级1；验证连续升级与存档恢复。
- 卸妆多剧本时单击选择一个失效，孙乾只在失效时抽1/2张牌，激活固定给予2临时敏捷。

- `KillingIntentTargetingTests`：三人战斗中升级杀意感知让每名盟友各选至多一张攻击；模组随机多目标及原版随机多段攻击必含指定敌人，目标死亡后停止代打。
- `TeleprompterRelicTests`：每三次加入剧本集抽两张，重排、素材强化和失效不计数，移出后再次加入计数。

- `VigorRefundStackingTests`：接着奏乐接着舞、杂兵、刀斧手任意两种和三种效果独立叠加返还，覆盖能力顺序、击杀条件及能量返还。
- `KeepCalmTests`：未被格挡的伤害只转化超过基础1点的蓄能，重复受伤不重复生成愤怒，完全格挡保留蓄能。

- 关帝形态不再具有剧本标签，不参与剧本专属生成/检索/减费；打出后先添加两种剧本并增加上限，再重选激活。瓦库代打自动选择互不冲突的合法剧本，不打开手动界面。
