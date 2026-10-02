# 孙乾宇宙：模组联动 API

孙乾宇宙通过 `Squ.Api.SunqianUniversePublicApi` 提供可选联动接口。推荐调用方使用 RitsuLib 的 `AssemblyInterop`，这样无需在项目中硬引用 `sunqian-universe.dll`。

## 调用方代理

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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

如果更偏好按模组 ID 绑定，也可以把特性替换为：

```csharp
[ModInterop("sunqian-universe", "Squ.Api.SunqianUniversePublicApi")]
```
