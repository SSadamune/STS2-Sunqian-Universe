using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

internal static class TestGuanYuCivilBlockOrder
{
    public static Creature? Target { get; set; }

    public static List<decimal> Amounts { get; } = [];

    public static void Reset()
    {
        Target = null;
        Amounts.Clear();
    }
}

[HarmonyPatch(typeof(Creature), "GainBlockInternal")]
internal static class TestGuanYuCivilBlockOrderPatch
{
    private static void Prefix(Creature __instance, decimal amount)
    {
        if (ReferenceEquals(__instance, TestGuanYuCivilBlockOrder.Target))
        {
            TestGuanYuCivilBlockOrder.Amounts.Add(amount);
        }
    }
}

public sealed class GuanDiFormTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("guan-di-form");

    [Fact]
    public async Task Upgraded_civil_choice_installs_form_and_fetches_a_free_sunqian_script()
    {
        var script = await AddCardToPile<SunqianScript>(PileType.Draw);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is GuanYuCivilVer { IsUpgraded: true });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GuanDiForm>()));
        }

        AssertAllSelectionsConsumed(selector);
        var form = Assert.IsType<GuanYuCivilVerPower>(
            Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.Equal(GuanYuCivilVer.BlockPerEnergy, form.Amount);
        AssertInPile(script, PileType.Hand);
        Assert.Equal(0, script.EnergyCost.GetResolved());
        Assert.Equal(0, Player.Creature.Block);

        await form.BeforeSideTurnEndEarly(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);

        Assert.Equal(GuanYuCivilVer.BlockPerEnergy * 3, Player.Creature.Block);
    }

    [Fact]
    public async Task Civil_form_aggregates_spent_energy_then_applies_dexterity_once_at_turn_end()
    {
        await ApplyPower<GuanYuCivilVerPower>(
            Player.Creature,
            GuanYuCivilVer.BlockPerEnergy);
        await ApplyPower<DexterityPower>(Player.Creature, 3);
        var form = Assert.IsType<GuanYuCivilVerPower>(
            Player.Creature.GetPower<GuanYuCivilVerPower>());
        CardModel first = await AddToHand<StrikeIronclad>();
        CardModel second = await AddToHand<DefendIronclad>();

        await form.AfterEnergySpent(first, 1);
        form.TrackEnergySpent(first, 1);
        await form.AfterEnergySpent(second, 2);

        Assert.Equal(0, Player.Creature.Block);
        var description = form.SmartDescription;
        description.Add("Amount", form.Amount);
        form.DynamicVars.AddTo(description);
        Assert.Contains("9", description.GetFormattedText());

        await form.BeforeSideTurnEndEarly(
            new BlockingPlayerChoiceContext(),
            CombatSide.Enemy,
            [EnemyAt(0)]);
        Assert.Equal(0, Player.Creature.Block);

        await form.BeforeSideTurnEndEarly(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);
        Assert.Equal(9, Player.Creature.Block);

        await form.BeforeSideTurnEndEarly(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);
        Assert.Equal(9, Player.Creature.Block);
    }

    [Fact]
    public async Task Civil_form_and_plating_trigger_in_the_order_the_powers_were_gained()
    {
        await ApplyPower<PlatingPower>(Player.Creature, 5);
        await ApplyPower<GuanYuCivilVerPower>(
            Player.Creature,
            GuanYuCivilVer.BlockPerEnergy);
        GuanYuCivilVerPower form = Assert.IsType<GuanYuCivilVerPower>(
            Player.Creature.GetPower<GuanYuCivilVerPower>());
        await form.AfterEnergySpent(await AddToHand<StrikeIronclad>(), 1);

        TestGuanYuCivilBlockOrder.Target = Player.Creature;
        try
        {
            await Hook.BeforeSideTurnEnd(
                Combat,
                CombatSide.Player,
                [Player.Creature]);

            Assert.Equal([5m, 2m], TestGuanYuCivilBlockOrder.Amounts);
        }
        finally
        {
            TestGuanYuCivilBlockOrder.Reset();
        }
    }

    [Fact]
    public async Task Civil_form_triggers_before_plating_when_it_was_gained_first()
    {
        await ApplyPower<GuanYuCivilVerPower>(
            Player.Creature,
            GuanYuCivilVer.BlockPerEnergy);
        await ApplyPower<PlatingPower>(Player.Creature, 5);
        GuanYuCivilVerPower form = Assert.IsType<GuanYuCivilVerPower>(
            Player.Creature.GetPower<GuanYuCivilVerPower>());
        await form.AfterEnergySpent(await AddToHand<StrikeIronclad>(), 1);

        TestGuanYuCivilBlockOrder.Target = Player.Creature;
        try
        {
            await Hook.BeforeSideTurnEnd(
                Combat,
                CombatSide.Player,
                [Player.Creature]);

            Assert.Equal([2m, 5m], TestGuanYuCivilBlockOrder.Amounts);
        }
        finally
        {
            TestGuanYuCivilBlockOrder.Reset();
        }
    }

    [Fact]
    public async Task Upgraded_martial_choice_converts_the_form_cards_energy_into_vigor()
    {
        await PlayerCmd.SetEnergy(3, Player);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is GuanYuMartialVer { IsUpgraded: true });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GuanDiForm>()));
        }

        AssertAllSelectionsConsumed(selector);
        var form = Assert.IsType<GuanYuMartialVerPower>(
            Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Equal(GuanYuMartialVer.UpgradedVigorPerEnergy, form.Amount);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            GuanYuMartialVer.UpgradedVigorPerEnergy * 3);
        CardTestAssertions.AssertPowerAmount<VigorAmplificationPower>(
            Player.Creature,
            VigorAmplificationPower.BonusStacksPerForm);
    }
}
