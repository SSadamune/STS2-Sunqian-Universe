#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Powers;

[RegisterPower]
public sealed class SovereignFormPower : ModPowerTemplate
{
    private static readonly ConditionalWeakTable<CardModel, object> StrikePreviews = new();

    // A detached native-card clone keeps the preview identical to SovereignBlade
    // without registering a new card or adding anything to the player's piles.
    internal static CardHoverTip CreateStrikePreview()
    {
        var preview = ModelDb.Card<SovereignBlade>().ToMutable();
        StrikePreviews.Add(preview, new object());
        return new CardHoverTip(preview);
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPower.png",
        BigIconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPowerBig.png");
    private static void Refresh(Creature creature)
    {
        if (creature.Player?.PlayerCombatState is not { } state) return;
        foreach (var blade in state.AllCards.OfType<SovereignBlade>())
            if (blade.Pile is { } pile) NCard.FindOnTable(blade)?.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
    }
    public override Task AfterApplied(Creature? applier, CardModel? source) { Refresh(Owner); return Task.CompletedTask; }
    public override Task AfterRemoved(Creature oldOwner) { Refresh(oldOwner); return Task.CompletedTask; }
    internal static bool Affects(CardModel card) => StrikePreviews.TryGetValue(card, out _)
        || (card is SovereignBlade && card.IsMutable
            && card.Owner?.Creature.HasPower<SovereignFormPower>() == true);
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
internal static class SovereignStrikeTitlePatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (!SovereignFormPower.Affects(__instance)) return;
        string title = new LocString("cards", "SUNQIAN_UNIVERSE_SOVEREIGN_STRIKE.title").GetFormattedText();
        __result = title + (__instance.IsUpgraded ? (__instance.MaxUpgradeLevel > 1 ? "+" + __instance.CurrentUpgradeLevel : "+") : "");
    }
}
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Tags), MethodType.Getter)]
internal static class SovereignStrikeTagPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref IEnumerable<CardTag> __result)
    {
        if (SovereignFormPower.Affects(__instance)) __result = __result.Append(CardTag.Strike).Distinct();
    }
}
