# 联合模组测试

联合测试会在真实游戏进程中同时启用孙乾宇宙、一将成名、新三国和 RitsuLib，并验证：

- 武诸葛使用《上方谷之叹》后，“好火啊……”会提高一将成名“火攻”的给予层数。
- 新三国角色使用《酒》时会获得“酒力”，而不是原版“活力”。
- 当双人队伍中的所有玩家都不是对应模组角色时，两张牌保持原效果，且不显示对应模组的能力悬停项或紫色联动说明。

一将成名测试要求安装版本已经公开 `Yjcm.Api.YjcmPublicApi`。尚未包含该 API 的旧正式版会明确失败；孙乾宇宙不会通过程序集名称或模型类型名绕过这一版本前提。

在仓库根目录执行：

```powershell
.\tests\run-joint-mod-tests.ps1
```

脚本默认读取以下 Steam Workshop 正式版目录：

- 一将成名：`C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3782162360`
- 新三国：`C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3784364146`

可以通过 `-YjcmDir`、`-NewsanguoDir`、`-RitsuLibDir` 和 `-GameDir` 覆盖路径。脚本会校验清单中的模组 ID，然后把正式版文件同步到游戏的 `mods` 目录。联合测试源文件仅在该入口设置 `Sts2JointModTests=true` 时编译；普通的 `run-tests.ps1` 不要求安装这两个外部模组。
