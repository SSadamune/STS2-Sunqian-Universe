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
using MegaCrit.Sts2.Core.Models.Relics;
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
    public async Task Vakuu_auto_play_selects_legal_scripts_without_waiting_for_manual_selection(bool existingRecords)
    {
        var form = await AddToHand<GuanDiForm>();
        if (existingRecords)
        {
            var context = new BlockingPlayerChoiceContext();
            await Squ.Script.ScriptSystem.UnlockOrReinforceAsync<GuanYuCivilVer>(context, form, 2, activate: false);
            await Squ.Script.ScriptSystem.UnlockOrReinforceAsync<GuanYuMartialVer>(context, form, 2, activate: false);
            var servant = await AddToHand<ServantScript>();
            await Squ.Script.ScriptSystem.UnlockOrReinforceAsync<ServantScriptRecord>(context, servant, 1, activate: false);
        }
        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), form, null).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.Null(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Equal(existingRecords ? 2 : 1, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.Equal(existingRecords, Player.Creature.GetPower<ScriptServantPower>() is not null);
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
        Assert.Equal(existingRecords ? (1, 2) : (1, 1), Squ.Script.ScriptSystem.GetSelectionBounds(Player, records));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Play_adds_both_scripts_and_one_activation_slot(bool upgraded)
    {
        var sunqian = await AddToDrawPileTop<SunqianScript>();
        var card = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(card);
        Assert.Equal(3, card.EnergyCost.GetResolved());
        var selector = new ScriptedCardSelector().ThenSelectWhere(candidate => candidate is GuanYuCivilVer);
        using (UseCardSelector(selector)) { await Play(card); }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(1, selector.SelectionRequestCount);
        Assert.DoesNotContain(SquCardTags.Script, card.Tags);
        Assert.DoesNotContain(SquKeywords.Script, card.Keywords);
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        var civil = Assert.Single(records.OfType<GuanYuCivilVer>());
        var martial = Assert.Single(records.OfType<GuanYuMartialVer>());
        Assert.Equal(2, records.Count);
        Assert.True(civil.CountsTowardActivationLimit);
        Assert.True(martial.CountsTowardActivationLimit);
        Assert.False(civil.ShouldShowInCardLibrary);
        Assert.False(martial.ShouldShowInCardLibrary);
        Assert.Equal(upgraded ? 3 : 2, civil.StoredAmount);
        Assert.Equal(upgraded ? 3 : 2, martial.StoredAmount);
        Assert.Equal(2, Squ.Script.ScriptSystem.GetActivationLimit(Player));
        AssertOnlyScript<GuanYuCivilVerPower>();
        AssertInPile(sunqian, PileType.Draw);
        Assert.Equal(0, Player.Creature.Block);
    }

    [Fact]
    public async Task Form_and_full_regular_slots_coexist_and_switching_forms_only_removes_the_other_form()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<ServantScript>());
        await PlayForm(await AddToHand<GuanDiForm>());
        await Activate<GuanYuCivilVer>();
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        await Play(await AddToHand<SunqianScript>());
        Assert.Equal(3, Squ.Script.ScriptSystem.GetActivationLimit(Player));
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
        await PlayForm(await AddToHand<GuanDiForm>());
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        await ApplyPower<DexterityPower>(Player.Creature, 3);
        await Activate<GuanYuCivilVer>();
        var form = Player.Creature.GetPower<GuanYuCivilVerPower>()!;
        var description = form.SmartDescription;
        description.Add("Amount", form.Amount);
        description.Add("energyPrefix", "");
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

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    public async Task Martial_gains_vigor_after_card_resolution_for_actual_energy(bool upgraded, int vigor)
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(card);
        await PlayForm(card);
        await Activate<GuanYuMartialVer>();
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        await Play(await AddToHand<DefendIronclad>());
        Assert.Equal(vigor, Player.Creature.GetPower<VigorPower>()!.Amount);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(hp - 6 - vigor, EnemyAt(0).CurrentHp);
        Assert.Equal(vigor, Player.Creature.GetPower<VigorPower>()!.Amount);
        var free = await AddToHand<DefendIronclad>();
        free.EnergyCost.SetUntilPlayed(0);
        await Play(free);
        Assert.Equal(vigor, Player.Creature.GetPower<VigorPower>()!.Amount);
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
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
        await PlayForm(await AddToHand<GuanDiForm>());
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Martial_grants_total_vigor_once_so_buddy_hu_converts_only_once(bool upgraded, bool xCost)
    {
        await PlayerCmd.SetEnergy(10, Player);
        var form = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(form);
        await PlayForm(form);
        await Activate<GuanYuMartialVer>();
        await ApplyPower<BuddyHuPower>(Player.Creature, 1);
        await PlayerCmd.SetEnergy(3, Player);
        if (xCost)
            await Play(await AddToHand<Whirlwind>());
        else
        {
            var card = await AddToHand<DefendIronclad>();
            card.EnergyCost.SetUntilPlayed(3);
            await Play(card);
        }
        Assert.Equal((upgraded ? 3 : 2) * 3 - 1, Player.Creature.GetPower<VigorPower>()!.Amount);
        Assert.Equal(1, Player.Creature.GetPower<StrengthPower>()!.Amount);
    }

    private async Task PlayForm(GuanDiForm card)
    {
        var selector = new ScriptedCardSelector().ThenSelect(options => options
            .Where(record => record is not GuanYuMartialVer)
            .Take(Squ.Script.ScriptSystem.GetActivationLimit(Player)).ToArray());
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
    }

    private async Task Activate<TRecord>() where TRecord : ScriptRecordCardTemplate =>
        await Squ.Script.ScriptSystem.ActivatePlayedScriptAsync(new BlockingPlayerChoiceContext(), Player,
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TRecord>().Single());

    private async Task SetupMartial()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await PlayForm(await AddToHand<GuanDiForm>());
        await Activate<GuanYuMartialVer>();
    }
}
