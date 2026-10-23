using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    public async Task Unlocks_records_and_keeps_the_same_selected_script_active()
    {
        await PlaySelecting<NeutralAmbushScript, NeutralAmbushScriptRecord>();
        var firstPower = Assert.IsType<ScriptNeutralAmbushPower>(
            Player.Creature.GetPower<ScriptNeutralAmbushPower>());

        await PlaySelecting<ServantScript, NeutralAmbushScriptRecord>();

        CardModel[] records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.ToArray();
        Assert.Contains(records, card => card is NeutralAmbushScriptRecord);
        Assert.Contains(records, card => card is ServantScriptRecord);
        Assert.Same(firstPower, Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Playing_a_reinforceable_script_again_strengthens_one_existing_record()
    {
        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();
        var firstPower = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());

        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();

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
        await PlaySelecting<SunqianScript, SunqianScriptRecord>();
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        var drawn = await AddToDrawPileTop<DefendIronclad>();

        await PlaySelecting<ServantScript, ServantScriptRecord>();

        Assert.Null(Player.Creature.GetPower<DexterityPower>());
        AssertInPile(drawn, PileType.Hand);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Replaying_the_active_eunuch_tracks_new_messages_without_invalidating_it()
    {
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        await PlaySelecting<EunuchScript, EunuchScriptRecord>();
        var active = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel third = await AddToDrawPileTop<Bash>();
        CardModel fourth = await AddToDrawPileTop<Wound>();
		await PlaySelecting<EunuchScript, EunuchScriptRecord>();

		Assert.Same(active, Player.Creature.GetPower<ScriptEunuchPower>());
		await PlayerCmd.SetEnergy(3, Player);
		await PlaySelecting<ServantScript, ServantScriptRecord>();

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

        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();
        Assert.Equal(1, Player.PlayerCombatState!.Energy);

        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();
        Assert.Equal(1, Player.PlayerCombatState.Energy);
    }

    [Fact]
    public async Task Abundant_box_lunch_rewards_every_play_and_adds_a_slot_per_three_unlocks()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(0, Player);

        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();
        await PlaySelecting<ExecutionCommanderScript, ExecutionCommanderScriptRecord>();
        Assert.Equal(2, Player.PlayerCombatState!.Energy);

        await PlayerCmd.SetEnergy(10, Player);
        await PlaySelecting<NeutralAmbushScript, NeutralAmbushScriptRecord>();

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(
                card => card is NeutralAmbushScriptRecord,
                card => card is ServantScriptRecord);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<ServantScript>());
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
    }

    private async Task PlaySelecting<TScript, TSelectedRecord>()
        where TScript : ScriptCardTemplate
        where TSelectedRecord : ScriptRecordCardTemplate
    {
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is TSelectedRecord);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<TScript>());
        }
        AssertAllSelectionsConsumed(selector);
    }
}
