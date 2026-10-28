using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

internal static class TestSighPlayPreview
{
    internal static CardModel? Target;
    internal static string? Description;
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.AfterApplied))]
internal static class TestSighPlayPreviewPatch
{
    private static void Postfix(PowerModel __instance, CardModel? cardSource)
    {
        if (__instance is GoodFirePower && ReferenceEquals(cardSource, TestSighPlayPreview.Target)
            && cardSource is ShangfangguSigh)
        {
            cardSource.DynamicVars[nameof(GoodFirePower)].UpdateCardPreview(cardSource, CardPreviewMode.Normal, null, true);
            TestSighPlayPreview.Description = cardSource.GetDescriptionForPile(PileType.Play);
        }
    }
}

public sealed class BurningLinkedCampsBonusTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("burning-linked-camps-bonus");

    [Theory]
    [InlineData(false, 100)]
    [InlineData(true, 150)]
    public async Task Hidden_bonus_stacks_and_only_the_next_sigh_uses_it_with_green_hand_and_play_previews(
        bool upgraded, int printed)
    {
        await ApplyPower<ScriptBurningLinkedCampsPower>(Player.Creature, 1);
        await TakeBurnDamage();
        await TakeBurnDamage();
        var pending = Player.Creature.GetPower<PendingGoodFirePower>()!;
        Assert.Equal(40, pending.Amount);
        Assert.False(pending.IsVisible);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
        var sigh = await AddToHand<ShangfangguSigh>();
        if (upgraded) Upgrade(sigh);
        Preview(sigh);
        Assert.Equal(printed, sigh.DynamicVars[nameof(GoodFirePower)].BaseValue);
        Assert.Contains($"[green]{printed + 40}[/green]", sigh.GetDescriptionForPile(PileType.Hand));
        TestSighPlayPreview.Target = sigh;
        TestSighPlayPreview.Description = null;
        try
        {
            await Play(sigh);
            Assert.NotNull(TestSighPlayPreview.Description);
            Assert.Contains($"[green]{printed + 40}[/green]", TestSighPlayPreview.Description!);
        }
        finally { TestSighPlayPreview.Target = null; TestSighPlayPreview.Description = null; }
        CardTestAssertions.AssertPowerAmount<GoodFirePower>(Player.Creature, printed + 40);
        Assert.Null(Player.Creature.GetPower<PendingGoodFirePower>());
        var next = await AddToHand<ShangfangguSigh>();
        Preview(next);
        Assert.Equal(100, next.DynamicVars[nameof(GoodFirePower)].PreviewValue);
        await Play(next);
        CardTestAssertions.AssertPowerAmount<GoodFirePower>(Player.Creature, printed + 140);
    }

    [Fact]
    public async Task Existing_good_fire_gains_twenty_percentage_points_per_burn_without_creating_hidden_bonus()
    {
        await ApplyPower<ScriptBurningLinkedCampsPower>(Player.Creature, 1);
        await ApplyPower<GoodFirePower>(Player.Creature, 100);
        await TakeBurnDamage();
        CardTestAssertions.AssertPowerAmount<GoodFirePower>(Player.Creature, 120);
        await TakeBurnDamage();
        CardTestAssertions.AssertPowerAmount<GoodFirePower>(Player.Creature, 140);
        Assert.Null(Player.Creature.GetPower<PendingGoodFirePower>());
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Hidden_bonus_survives_script_ending_and_other_cards_but_only_previews_in_hand_or_play()
    {
        await ApplyPower<ScriptBurningLinkedCampsPower>(Player.Creature, 1);
        await TakeBurnDamage();
        await Squ.Script.ScriptSystem.InvalidateScriptsAsync(Player.Creature);
        await Play(await AddToHand<DefendIronclad>());
        CardTestAssertions.AssertPowerAmount<PendingGoodFirePower>(Player.Creature, 20);
        var sigh = await AddToDrawPileTop<ShangfangguSigh>();
        Preview(sigh);
        Assert.Equal(100, sigh.DynamicVars[nameof(GoodFirePower)].PreviewValue);
        await CardPileCmd.Add(sigh, PileType.Hand);
        Preview(sigh);
        Assert.Contains("[green]120[/green]", sigh.GetDescriptionForPile(PileType.Hand));
        await Play(sigh);
        CardTestAssertions.AssertPowerAmount<GoodFirePower>(Player.Creature, 120);
    }

    [Fact]
    public async Task Non_burn_damage_does_not_increase_good_fire_or_create_hidden_bonus()
    {
        await ApplyPower<ScriptBurningLinkedCampsPower>(Player.Creature, 1);
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Player.Creature, 2, ValueProp.Unpowered, null, null);
        Assert.Null(Player.Creature.GetPower<GoodFirePower>());
        Assert.Null(Player.Creature.GetPower<PendingGoodFirePower>());
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public void Script_record_has_the_burning_hover_tip_and_updated_description()
    {
        var record = ModelDb.Card<BurningLinkedCampsScriptRecord>();
        var burning = HoverTipFactory.FromPower<BurningPower>();
        Assert.Contains(record.HoverTips, tip => tip.Id == burning.Id);
        Assert.Contains("效果增加20%。\n", record.GetDescriptionForPile(PileType.Deck));
    }

    private async Task TakeBurnDamage() => await CreatureCmd.Damage(
        new BlockingPlayerChoiceContext(), Player.Creature, 2, ValueProp.Unpowered, Combat.CreateCard<Burn>(Player), null);

    private static void Preview(CardModel card) => card.DynamicVars[nameof(GoodFirePower)]
        .UpdateCardPreview(card, CardPreviewMode.Normal, null, true);
}
