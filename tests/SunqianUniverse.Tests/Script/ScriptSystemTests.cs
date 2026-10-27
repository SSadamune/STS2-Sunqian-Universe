using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Squ.Cards;
using Squ.Patches;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class ScriptSystemTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-system-rework");

    [Fact]
    public async Task Playing_a_new_script_auto_activates_it_and_replaces_the_only_active_script()
    {
        await PlayScript<NeutralAmbushScript>();
        NeutralAmbushScriptRecord neutralRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<NeutralAmbushScriptRecord>());
        Assert.True(neutralRecord.ShouldGlowGold);

        await PlayScript<ServantScript>();

        CardModel[] records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.ToArray();
        Assert.Contains(records, card => card is NeutralAmbushScriptRecord);
        Assert.Contains(records, card => card is ServantScriptRecord);
        Assert.Null(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.False(neutralRecord.ShouldGlowGold);
        Assert.True(Assert.Single(records.OfType<ServantScriptRecord>()).ShouldGlowGold);
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Playing_a_reinforceable_script_again_strengthens_one_existing_record()
    {
        await PlayScript<ExecutionCommanderScript>();
        var firstPower = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());

        await PlayScript<ExecutionCommanderScript>();

        ExecutionCommanderScriptRecord record = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<ExecutionCommanderScriptRecord>());
        Assert.Equal(ExecutionCommanderScript.BaseBonusPercent * 2, record.StoredAmount);
        var reinforcedPower = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.NotSame(firstPower, reinforcedPower);
        Assert.Equal(record.StoredAmount, reinforcedPower.Amount);
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Sunqian_draws_on_play_and_its_temporary_dexterity_survives_switching_out()
    {
        var drawn = await AddToDrawPileTop<DefendIronclad>();
        await PlayScript<SunqianScript>();
        AssertInPile(drawn, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        var staysInDrawPile = await AddToDrawPileTop<StrikeIronclad>();

        await PlayScript<ServantScript>();

        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        AssertInPile(staysInDrawPile, PileType.Draw);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Upgraded_sunqian_draws_two_and_grants_three_temporary_dexterity()
    {
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();

        await Play(Upgrade(await AddToHand<SunqianScript>()));

        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.UpgradedDexterity);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.UpgradedDexterity);
    }

    [Fact]
    public async Task Replaying_the_active_eunuch_rebuilds_the_power_and_both_plays_keep_their_drawn_cards()
    {
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        await PlayScript<EunuchScript>();
        var active = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel third = await AddToDrawPileTop<Bash>();
        CardModel fourth = await AddToDrawPileTop<Wound>();
        await PlayScript<EunuchScript>();

        Assert.NotSame(active, Player.Creature.GetPower<ScriptEunuchPower>());
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        AssertInPile(third, PileType.Hand);
        AssertInPile(fourth, PileType.Hand);
        Assert.True(first.HasEunuchMessage());
        Assert.True(second.HasEunuchMessage());
        Assert.True(third.HasEunuchMessage());
        Assert.True(fourth.HasEunuchMessage());
    }

    [Fact]
    public async Task Box_lunch_reselecting_active_eunuch_rebuilds_power_without_drawing_or_moving_cards()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        CardModel oldSecond = await AddToDrawPileTop<DefendIronclad>();
        CardModel oldFirst = await AddToDrawPileTop<StrikeIronclad>();
        await PlayScript<EunuchScript>();
        var oldPower = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel newSecond = await AddToDrawPileTop<Wound>();
        CardModel newFirst = await AddToDrawPileTop<Bash>();
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector))
        {
            await relic.AfterSideTurnStart(
                CombatSide.Player,
                [Player.Creature],
                Combat);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.NotSame(oldPower, Player.Creature.GetPower<ScriptEunuchPower>());
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
        AssertInPile(oldFirst, PileType.Hand);
        AssertInPile(oldSecond, PileType.Hand);
        AssertInPile(newFirst, PileType.Draw);
        AssertInPile(newSecond, PileType.Draw);
        Assert.True(oldFirst.HasEunuchMessage());
        Assert.True(oldSecond.HasEunuchMessage());
        Assert.False(newFirst.HasEunuchMessage());
        Assert.False(newSecond.HasEunuchMessage());
    }

    [Fact]
    public async Task Box_lunch_requires_one_selection_when_two_scripts_are_available()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<SunqianScript>();
        await PlayScript<ExecutionCommanderScript>();
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is SunqianScriptRecord);

        using (UseCardSelector(selector))
        {
            await relic.AfterSideTurnStart(
                CombatSide.Player,
                [Player.Creature],
                Combat);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(1, selector.SelectionRequestCount);
        Assert.Equal(1, selector.LastMinSelect);
        Assert.Equal(1, selector.LastMaxSelect);
        AssertOnlyScript<ScriptSunqianPower>();
    }

    [Fact]
    public async Task Box_lunch_reselecting_reinforceable_script_rebuilds_without_deactivation()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<ExecutionCommanderScript>();
        var active = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        var selector = new ScriptedCardSelector();

        using (UseCardSelector(selector))
        {
            await relic.AfterSideTurnStart(
                CombatSide.Player,
                [Player.Creature],
                Combat);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.NotSame(active, Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Box_lunch_adds_exactly_one_slot_once_four_scripts_are_unlocked()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(0, Player);

        await PlayScript<ExecutionCommanderScript>();
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.Equal(1, ScriptSystem.GetActivationLimit(Player));

        await PlayerCmd.SetEnergy(10, Player);
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        await PlayScript<SunqianScript>();

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(1, relic.GetAdditionalScriptActivationSlots(Player, 8));
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
    }

    [Fact]
    public async Task Abundant_box_lunch_gains_energy_per_four_scripts_and_pays_for_each_excess_selection()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);
        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        await PlayScript<SunqianScript>();

        Assert.Equal(6, Player.PlayerCombatState!.Energy);
        Assert.Equal(1, ScriptSystem.GetActivationLimit(Player));

        await PlayerCmd.SetEnergy(1, Player);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(
                card => card is ExecutionCommanderScriptRecord,
                card => card is NeutralAmbushScriptRecord,
                card => card is ServantScriptRecord);
        using (UseCardSelector(selector))
        {
            await relic.AfterSideTurnStart(
                CombatSide.Player,
                [Player.Creature],
                Combat);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(1, selector.LastMinSelect);
        Assert.Equal(3, selector.LastMaxSelect);
        Assert.Equal(0, Player.PlayerCombatState.Energy);
        Assert.Equal(1, ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(3, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
    }

    [Fact]
    public void Abundant_box_lunch_selection_prompt_displays_the_current_energy_cost()
    {
        string prompt = ScriptSystem.CreatePaidSelectionPrompt(
            freeSelectionCount: 1,
            energyCost: 2,
            energyPrefix: "sunqian").GetFormattedText();

        Assert.Contains("当前花费", prompt);
        Assert.Contains("[blue]2[/blue]", prompt);
    }

    [Fact]
    public void Script_activation_selection_replaces_the_oldest_choice_when_full()
    {
        CardModel a = ModelDb.Card<SunqianScriptRecord>();
        CardModel b = ModelDb.Card<ServantScriptRecord>();
        CardModel c = ModelDb.Card<NeutralAmbushScriptRecord>();
        CardModel d = ModelDb.Card<GruntScriptRecord>();
        var selected = new HashSet<CardModel>();
        var order = new ScriptActivationSelectionOrder();

        Click(b);
        Click(c);
        Click(a);
        Click(d);

        Assert.DoesNotContain(b, selected);
        Assert.Equal([c, a, d], order.Cards);

        void Click(CardModel card)
        {
            CardModel? removed = order.MakeRoomFor(selected, card, maxSelect: 3);
            if (removed is not null)
            {
                selected.Remove(removed);
            }

            if (!selected.Remove(card))
            {
                selected.Add(card);
            }
            order.RecordResult(selected, card);
        }
    }

    [Fact]
    public void Selected_script_highlight_blue_takes_priority_over_active_white()
    {
        Assert.Equal(
            ScriptRecordCardHighlight.ActiveColor,
            ScriptRecordCardHighlight.ResolveColor(isActive: true, isSelected: false));
        Assert.Equal(
            NCardHighlight.playableColor,
            ScriptRecordCardHighlight.ResolveColor(isActive: true, isSelected: true));
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.R);
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.G);
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.B);
    }

    [Fact]
    public async Task Playing_a_new_script_with_multiple_active_scripts_asks_which_old_script_ends()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        await PlayScript<SunqianScript>();

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is ServantScriptRecord)
            .ThenSelectWhere(card => card is BowangParadoxVigorChoice);
        using (UseCardSelector(selector))
        {
            await PlayScript<BowangParadoxScript>();
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.Null(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxPower>());
    }

    [Fact]
    public async Task Active_record_highlight_tracks_manual_reselection()
    {
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        NeutralAmbushScriptRecord neutralRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<NeutralAmbushScriptRecord>());
        ServantScriptRecord servantRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<ServantScriptRecord>());
        Assert.False(neutralRecord.ShouldGlowGold);
        Assert.True(servantRecord.ShouldGlowGold);

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is NeutralAmbushScriptRecord);
        using (UseCardSelector(selector))
        {
            bool selected = await ScriptSystem.ChooseActiveScriptsAsync(
                new BlockingPlayerChoiceContext(),
                Player,
                cancelable: false);
            Assert.True(selected);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.True(neutralRecord.ShouldGlowGold);
        Assert.False(servantRecord.ShouldGlowGold);
    }

    [Fact]
    public async Task Manual_reselection_passes_its_choice_context_to_bowang_paradox()
    {
        var initialSelector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxVigorChoice);
        using (UseCardSelector(initialSelector))
        {
            await PlayScript<BowangParadoxScript>();
        }
        AssertAllSelectionsConsumed(initialSelector);

        CardModel burning = await AddToHand<BlockFirefighting>();
        var reselectSelector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxTinderChoice);
        using (UseCardSelector(reselectSelector))
        {
            bool selected = await ScriptSystem.ChooseActiveScriptsAsync(
                new BlockingPlayerChoiceContext(),
                Player,
                cancelable: false);
            Assert.True(selected);
        }
        AssertAllSelectionsConsumed(reselectSelector);

        AssertInPile(burning, PileType.Discard);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    private async Task PlayScript<TScript>()
        where TScript : ScriptCardTemplate
        => await Play(await AddToHand<TScript>());
}
