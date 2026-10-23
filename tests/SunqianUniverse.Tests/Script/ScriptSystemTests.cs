using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
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
    public async Task Sunqian_dexterity_is_removed_and_one_card_is_drawn_when_switched_out()
    {
        await PlayScript<SunqianScript>();
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        var drawn = await AddToDrawPileTop<DefendIronclad>();

        await PlayScript<ServantScript>();

        Assert.Null(Player.Creature.GetPower<DexterityPower>());
        AssertInPile(drawn, PileType.Hand);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Replaying_the_active_eunuch_tracks_new_messages_without_invalidating_it()
    {
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        await PlayScript<EunuchScript>();
        var active = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel third = await AddToDrawPileTop<Bash>();
        CardModel fourth = await AddToDrawPileTop<Wound>();
		await PlayScript<EunuchScript>();

		Assert.Same(active, Player.Creature.GetPower<ScriptEunuchPower>());
		await PlayerCmd.SetEnergy(3, Player);
		await PlayScript<ServantScript>();

        foreach (CardModel card in new[] { first, second, third, fourth })
        {
            AssertInPile(card, PileType.Exhaust);
        }
    }

    [Fact]
    public async Task Box_lunch_grants_energy_only_for_a_new_unlock()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(0, Player);

        await PlayScript<ExecutionCommanderScript>();
        Assert.Equal(1, Player.PlayerCombatState!.Energy);

        await PlayScript<ExecutionCommanderScript>();
        Assert.Equal(1, Player.PlayerCombatState.Energy);
    }

    [Fact]
    public async Task Abundant_box_lunch_rewards_every_play_and_adds_a_slot_per_three_unlocks()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(0, Player);

        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<ExecutionCommanderScript>();
        Assert.Equal(2, Player.PlayerCombatState!.Energy);

        await PlayerCmd.SetEnergy(10, Player);
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
    }

    [Fact]
    public async Task Playing_a_new_script_with_multiple_active_scripts_asks_which_old_script_ends()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is NeutralAmbushScriptRecord);
        using (UseCardSelector(selector))
        {
            await PlayScript<BowangParadoxScript>();
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.Null(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxPower>());
    }

    [Fact]
    public async Task Active_record_gold_glow_tracks_manual_reselection()
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

    private async Task PlayScript<TScript>()
        where TScript : ScriptCardTemplate
        => await Play(await AddToHand<TScript>());
}
