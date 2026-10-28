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
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("guan-di-scripts");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Play_adds_both_free_scripts_without_activating_or_fetching_sunqian(bool upgraded)
    {
        var sunqian = await AddToDrawPileTop<SunqianScript>();
        var card = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(card);
        Assert.Equal(3, card.EnergyCost.GetResolved());
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector)) { await Play(card); }
        Assert.Equal(0, selector.SelectionRequestCount);
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        var civil = Assert.Single(records.OfType<GuanYuCivilVer>());
        var martial = Assert.Single(records.OfType<GuanYuMartialVer>());
        Assert.Equal(2, records.Count);
        Assert.False(civil.CountsTowardActivationLimit);
        Assert.False(martial.CountsTowardActivationLimit);
        Assert.False(civil.ShouldShowInCardLibrary);
        Assert.False(martial.ShouldShowInCardLibrary);
        Assert.Equal(2, civil.StoredAmount);
        Assert.Equal(1, martial.StoredAmount);
        Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        AssertInPile(sunqian, PileType.Draw);
        Assert.Equal(0, Player.Creature.Block);
    }

    [Fact]
    public async Task Free_form_and_full_regular_slots_coexist_and_switching_forms_only_removes_the_other_form()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<GuanDiForm>());
        await Activate<GuanYuCivilVer>();
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        await Play(await AddToHand<SunqianScript>());
        Assert.Equal(2, Squ.Script.ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(3, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        await Activate<GuanYuMartialVer>();
        Assert.Null(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is ServantScriptRecord);
        using (UseCardSelector(selector)) { await Play(await AddToHand<GruntScript>(), EnemyAt(0)); }
        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Equal(3, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
    }

    [Fact]
    public async Task Civil_counts_energy_spent_before_activation_and_applies_dexterity_once()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<GuanDiForm>());
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        await ApplyPower<DexterityPower>(Player.Creature, 3);
        await Activate<GuanYuCivilVer>();
        var form = Player.Creature.GetPower<GuanYuCivilVerPower>()!;
        var description = form.SmartDescription;
        form.DynamicVars.AddTo(description);
        Assert.Contains("[blue]11[/blue]", description.GetFormattedText());
        await form.BeforeSideTurnEndEarly(new BlockingPlayerChoiceContext(), CombatSide.Enemy, [EnemyAt(0)]);
        Assert.Equal(0, Player.Creature.Block);
        await form.BeforeSideTurnEndEarly(new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(11, Player.Creature.Block);
        await form.BeforeSideTurnEndEarly(new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(11, Player.Creature.Block);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Civil_and_plating_trigger_in_the_order_the_powers_were_gained(bool platingFirst)
    {
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        if (platingFirst) await ApplyPower<PlatingPower>(Player.Creature, 5);
        await ApplyPower<GuanYuCivilVerPower>(Player.Creature, 2);
        if (!platingFirst) await ApplyPower<PlatingPower>(Player.Creature, 5);
        TestGuanYuCivilBlockOrder.Target = Player.Creature;
        try
        {
            await Hook.BeforeSideTurnEnd(Combat, CombatSide.Player, [Player.Creature]);
            Assert.Equal(platingFirst ? new decimal[] { 5, 2 } : new decimal[] { 2, 5 }, TestGuanYuCivilBlockOrder.Amounts);
        }
        finally { TestGuanYuCivilBlockOrder.Reset(); }
    }

    [Fact]
    public async Task Martial_amplifies_only_the_first_attack_and_no_longer_gains_vigor_per_energy()
    {
        await SetupMartial();
        await ApplyPower<VigorPower>(Player.Creature, 4);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(hp - 14, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        await ApplyPower<VigorPower>(Player.Creature, 4);
        hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(hp - 10, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Martial_activation_after_the_first_attack_does_not_amplify_later_attacks()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        await Play(await AddToHand<GuanDiForm>());
        await Activate<GuanYuMartialVer>();
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
        await ApplyPower<VigorPower>(Player.Creature, 4);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(hp - 10, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task First_attack_without_vigor_still_uses_martial_bonus_and_reactivation_does_not_reset_it()
    {
        await SetupMartial();
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
        await Activate<GuanYuMartialVer>();
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
        await ApplyPower<VigorPower>(Player.Creature, 4);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(hp - 10, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Next_turn_keeps_the_script_without_a_form_choice_or_automatic_martial_bonus()
    {
        await SetupMartial();
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector)) { await EndTurn(); }
        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
    }

    [Fact]
    public async Task Selection_allows_regular_scripts_plus_one_form_and_replaces_conflicting_form_on_click()
    {
        var relic = (Squ.Relics.BoxLunchRelic)ModelDb.Relic<Squ.Relics.BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<GuanDiForm>());
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
        var civil = records.OfType<GuanYuCivilVer>().Single();
        var martial = records.OfType<GuanYuMartialVer>().Single();
        var servant = records.OfType<ServantScriptRecord>().Single();
        Assert.Equal((1, 2), Squ.Script.ScriptSystem.GetSelectionBounds(Player, records));
        Assert.True(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, civil]));
        Assert.True(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, martial]));
        Assert.False(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [civil, martial]));
        var anotherRegular = Combat.CreateCard<ExecutionCommanderScriptRecord>(Player);
        Assert.False(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, anotherRegular, civil]));
        Assert.Same(servant, Squ.Patches.CardGridSelectionRotation.FindScriptConstraintRemoval(
            new CardModel[] { servant, civil }, anotherRegular, new CardModel[] { servant, civil }));
        Assert.Same(civil, Squ.Patches.CardGridSelectionRotation.FindScriptConstraintRemoval(
            new CardModel[] { servant, civil }, martial, new CardModel[] { servant, civil }));
        var selector = new ScriptedCardSelector().ThenSelect(servant, civil);
        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
        Assert.NotNull(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Null(Player.Creature.GetPower<GuanYuMartialVerPower>());
    }

    private async Task Activate<TRecord>() where TRecord : ScriptRecordCardTemplate =>
        await Squ.Script.ScriptSystem.ActivatePlayedScriptAsync(new BlockingPlayerChoiceContext(), Player,
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TRecord>().Single());

    private async Task SetupMartial()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<GuanDiForm>());
        await Activate<GuanYuMartialVer>();
    }
}
