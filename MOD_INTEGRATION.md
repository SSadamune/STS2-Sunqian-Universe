# 孙乾宇宙：模组联动 API

孙乾宇宙通过 `Squ.Api.SunqianUniversePublicApi` 提供可选联动接口。推荐调用方使用 RitsuLib 的 `AssemblyInterop`，这样无需在项目中硬引用 `sunqian-universe.dll`。

## 调用方代理

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop;

[AssemblyInterop("Squ.Api.SunqianUniversePublicApi, sunqian-universe")]
public static class SunqianUniverseApiInterop
{
    public static bool IsReady => false;

    public static bool IsSupportingActorCharacter(CharacterModel? character) => false;
    public static bool HasSupportingActorInCombat() => false;

    public static bool IsBurningPower(PowerModel? power) => false;
    public static IHoverTip CreateBurningHoverTip() => null!;
    public static Task<PowerModel?> ApplyBurning(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) => null!;

    public static bool IsScriptPower(PowerModel? power) => false;
    public static PowerModel? GetActiveScript(Creature? creature) => null;
    public static bool HasActiveScript(Creature? creature) => false;
    public static Task ApplyScriptPower(
        PlayerChoiceContext choiceContext,
        PowerModel scriptPower,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) => Task.CompletedTask;
    public static Task InvalidateScripts(Creature creature) => Task.CompletedTask;
    public static Task RemoveScript(PowerModel power, bool notifyLift = true) => Task.CompletedTask;
    public static int GetScriptLiftsThisTurn(Player? player) => 0;

    public static string SlightRevisionKeywordId => "";
    public static CardKeyword SlightRevisionKeyword => default;
    public static IHoverTip CreateSlightRevisionHoverTip() => null!;
    public static bool GrantSlightRevision(
        CardModel card,
        CardModel target,
        bool targetUpgraded = false) => false;
    public static void SetSlightRevision(
        CardModel card,
        CardModel target,
        bool targetUpgraded = false) { }
    public static bool HasSlightRevision(CardModel? card) => false;
    public static CardModel? GetSlightRevisionTarget(CardModel? card) => null;
    public static bool IsSlightRevisionTargetUpgraded(CardModel? card) => false;
    public static bool CanExecuteSlightRevision(Player player, CardModel? card) => false;
    public static bool RequestSlightRevision(Player player, CardModel card) => false;
    public static void AddSlightRevisionDescription(
        LocString description,
        CardModel target,
        bool targetUpgraded = false) { }
    public static IEnumerable<IHoverTip> GetSlightRevisionHoverTips(
        CardModel target,
        bool targetUpgraded = false) => null!;
}
```

本模组初始化时已调用 `ModTypeDiscoveryHub.RegisterModAssembly`。请始终先检查 `IsReady`，把未安装孙乾宇宙视为正常分支：

```csharp
if (SunqianUniverseApiInterop.IsReady &&
    SunqianUniverseApiInterop.HasSupportingActorInCombat())
{
    await SunqianUniverseApiInterop.ApplyBurning(
        choiceContext,
        target,
        6m,
        applier,
        cardSource);
}
```

`ApplyScriptPower` 只接受孙乾宇宙注册的剧本能力（即 API 的 `IsScriptPower` 返回 `true` 的能力），并执行本模组的剧本替换与失效生命周期。对当前剧本进行通用操作时，可使用 `GetActiveScript`、`HasActiveScript`、`InvalidateScripts` 和 `RemoveScript`。

## 让外部卡牌获得「稍作修改」

对战斗内的可变卡牌调用一次 `GrantSlightRevision` 即可。它会同时添加关键词和可持久化 capability；描述、目标牌悬停、存档、克隆及右键联机同步均由孙乾宇宙处理：

```csharp
if (SunqianUniverseApiInterop.IsReady)
{
    SunqianUniverseApiInterop.GrantSlightRevision(
        card,
        ModelDb.Card<MyRevisionTarget>(),
        targetUpgraded: card.IsUpgraded);
}
```

`GrantSlightRevision` 遵循统一的覆盖规则：如果卡牌已经有固有或动态的「稍作修改」，新目标会覆盖旧目标，关键词和 capability 不会重复。需要在来源卡升级时更新目标状态的固有卡牌，也可显式调用 `SetSlightRevision`；例如在卡牌的 `AfterCreated` 中配置一次，并在升级逻辑中再次调用它。

若固有卡牌还要在图鉴等规范实例界面展示完整信息，可以在自身的卡牌覆写中使用：

- `SlightRevisionKeyword`：在 `IsReady` 为 `true` 时加入 `CanonicalKeywords`。
- `AddSlightRevisionDescription`：注入 `{SlightRevisionText}` 描述参数。
- `GetSlightRevisionHoverTips`：展示目标牌悬停说明。

正常情况下无需调用 `RequestSlightRevision`；只要卡牌已通过 `GrantSlightRevision` 或 `SetSlightRevision` 配置，本模组的全局右键处理器就会自动接管。该方法只用于外部模组希望从其他交互入口主动请求同一联机动作的情况。

对孙乾宇宙采用硬依赖的模组也可以直接继承公开的 `SlightRevisionCardTemplate<TTarget>`，或实现 `ISlightRevisionSource`；可选依赖仍推荐使用上面的公开 API。

如果更偏好按模组 ID 绑定，也可以把特性替换为：

```csharp
[ModInterop("sunqian-universe", "Squ.Api.SunqianUniversePublicApi")]
```
