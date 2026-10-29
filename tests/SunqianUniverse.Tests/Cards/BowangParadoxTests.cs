using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Character;
using Squ.Patches;
using Squ.Script;
using Squ.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BowangParadoxTests : SunqianCombatTestSuite
{
    [Fact]
    public async Task First_play_records_both_scripts_but_activates_only_the_chosen_one()
    {
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());
        var records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        var advance = Assert.Single(records.OfType<BowangParadoxAdvanceScriptRecord>());
        var arson = Assert.Single(records.OfType<BowangParadoxArsonScriptRecord>());
        Assert.Equal(2, records.Count);
        Assert.Equal(BowangParadoxScript.BaseVigor, advance.StoredAmount);
        Assert.Equal(BowangParadoxScript.BaseTinderStacks, arson.StoredAmount);
        Assert.True(advance.CanBeReinforced);
        Assert.True(arson.CanBeReinforced);
        Assert.True(advance.ShouldGlowGold);
        Assert.False(arson.ShouldGlowGold);
        AssertOnlyScript<ScriptBowangParadoxAdvancePower>();
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Replay_upgrades_both_records_and_reactivates_the_chosen_script()
    {
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());
        var old = Player.Creature.GetPower<ScriptBowangParadoxAdvancePower>();
        await PlayerCmd.SetEnergy(10, Player);
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());
        Assert.NotSame(old, Player.Creature.GetPower<ScriptBowangParadoxAdvancePower>());
        Assert.All(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
            .OfType<ScriptRecordCardTemplate>(), record => Assert.Equal(9, record.StoredAmount));
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 16);
        Assert.Equal(2, ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Both_records_count_toward_the_four_script_threshold_and_can_be_activated_together()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await PlayAndActivateScript(await AddToHand<ServantScript>());
        await PlayAndActivateScript(await AddToHand<SunqianScript>());
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());
        Assert.Equal(4, ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.Count);
        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        var selector = new ScriptedCardSelector().ThenSelectWhere(
            card => card is BowangParadoxAdvanceScriptRecord,
            card => card is BowangParadoxArsonScriptRecord);
        using (UseCardSelector(selector))
        {
            Assert.True(await ScriptSystem.ChooseActiveScriptsAsync(
                new BlockingPlayerChoiceContext(), Player, cancelable: false));
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxAdvancePower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxArsonPower>());
    }

    [Fact]
    public async Task Box_lunch_reactivates_the_selected_independent_script_without_a_second_choice()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());
        var burning = await AddToHand<BlockFirefighting>();
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BowangParadoxArsonScriptRecord);
        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(1, selector.SelectionRequestCount);
        AssertInPile(burning, PileType.Discard);
        AssertOnlyScript<ScriptBowangParadoxArsonPower>();
    }

    [Fact]
    public void Both_script_records_use_the_purple_character_pool_and_script_type_plaque()
    {
        CardModel[] choices = [ModelDb.Card<BowangParadoxAdvanceScriptRecord>(), ModelDb.Card<BowangParadoxArsonScriptRecord>()];
        foreach (CardModel choice in choices)
        {
            Assert.Same(ModelDb.CardPool<SunqianCardPool>(), choice.VisualCardPool);
            Assert.True(ScriptRecordCardVisualPatch.UsesScriptTypePlaque(choice));
            Assert.False(choice.ShouldShowInCardLibrary);
        }
        Assert.False(ScriptRecordCardVisualPatch.UsesScriptTypePlaque(ModelDb.Card<DefendLongtao>()));
    }
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-bowang-paradox");
    }

    [Fact]
    public async Task BP_01_play_discards_only_unretained_attacks()
    {
        var unretainedAttack = await AddToHand<FlameStrike>();
        var retainedAttack = await AddToHand<FingerSnapStrike>();
        var burningSkill = await AddToHand<BlockFirefighting>();
        var ordinarySkill = await AddToHand<DefendLongtao>();
        var bowang = await AddToHand<BowangParadoxScript>();

        await PlayAndChooseVigor(bowang);

        AssertInPile(unretainedAttack, PileType.Discard);
        AssertInPile(retainedAttack, PileType.Hand);
        AssertInPile(burningSkill, PileType.Hand);
        AssertInPile(ordinarySkill, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            BowangParadoxScript.BaseVigor);
        AssertOnlyScript<ScriptBowangParadoxAdvancePower>();
    }

    [Fact]
    public async Task BP_07_upgraded_vigor_choice_grants_nine_vigor()
    {
        var bowang = Upgrade(await AddToHand<BowangParadoxScript>());

        await PlayAndChooseVigor(bowang);

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            BowangParadoxScript.UpgradedVigor);
        AssertOnlyScript<ScriptBowangParadoxAdvancePower>();
    }

    [Fact]
    public async Task BP_08_upgraded_tinder_choice_grants_nine_tinder()
    {
        var bowang = Upgrade(await AddToHand<BowangParadoxScript>());
        await PlayAndChooseTinder(bowang);

        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.UpgradedTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxArsonPower>();
    }

    [Fact]
    public async Task BP_02_single_turn_retain_prevents_play_time_discard()
    {
        var retainedAttack = GiveSingleTurnRetain(await AddToHand<FlameStrike>());
        var bowang = await AddToHand<BowangParadoxScript>();

        Assert.True(retainedAttack.ShouldRetainThisTurn);
        await PlayAndChooseVigor(bowang);

        AssertInPile(retainedAttack, PileType.Hand);
        AssertOnlyScript<ScriptBowangParadoxAdvancePower>();
    }

    [Fact]
    public async Task BP_03_expired_single_turn_retain_no_longer_prevents_discard()
    {
        var attack = GiveSingleTurnRetain(await AddToHand<FlameStrike>());

        Assert.True(attack.ShouldRetainThisTurn);
        await EndTurn();

        AssertInPile(attack, PileType.Hand);
        Assert.False(attack.ShouldRetainThisTurn);

        var bowang = await AddToHand<BowangParadoxScript>();
        await PlayAndChooseVigor(bowang);

        AssertInPile(attack, PileType.Discard);
        AssertOnlyScript<ScriptBowangParadoxAdvancePower>();
    }

    [Fact]
    public async Task BP_04_tinder_choice_discards_only_unretained_burning_cards()
    {
        var burningAttack = await AddToHand<FlameStrike>();
        var burningSkill = await AddToHand<BlockFirefighting>();
        var retainedBurning = GiveSingleTurnRetain(await AddToHand<FlameStrike>());
        var nonBurningSkill = await AddToHand<DefendLongtao>();
        var bowang = await AddToHand<BowangParadoxScript>();

        await PlayAndChooseTinder(bowang);

        AssertInPile(burningAttack, PileType.Discard);
        AssertInPile(burningSkill, PileType.Discard);
        AssertInPile(retainedBurning, PileType.Hand);
        AssertInPile(nonBurningSkill, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxArsonPower>();
    }

    [Fact]
    public async Task BP_05_deactivation_has_no_effect()
    {
        var bowang = await AddToHand<BowangParadoxScript>();
        await PlayAndChooseVigor(bowang);

        var burningSkill = await AddToHand<BlockFirefighting>();
        var replacement = await AddToHand<ServantScript>();

        await PlayAndActivateScript(replacement);

        AssertInPile(burningSkill, PileType.Hand);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task BP_06_reactivation_offers_a_fresh_choice()
    {
        await PlayAndChooseVigor(await AddToHand<BowangParadoxScript>());

        var burningSkill = await AddToHand<BlockFirefighting>();
        await PlayAndChooseTinder(await AddToHand<BowangParadoxScript>());

        AssertInPile(burningSkill, PileType.Discard);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            BowangParadoxScript.BaseVigor);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks + 2);
        BowangParadoxAdvanceScriptRecord record = Assert.Single(
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<BowangParadoxAdvanceScriptRecord>());
        Assert.Equal(BowangParadoxScript.BaseVigor + 2, record.StoredAmount);
        CardTestAssertions.AssertPowerAmount<ScriptBowangParadoxArsonPower>(
            Player.Creature,
            BowangParadoxScript.BaseVigor + 2);
        AssertOnlyScript<ScriptBowangParadoxArsonPower>();
    }

    private async Task PlayAndChooseVigor(BowangParadoxScript bowang)
    {
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxAdvanceScriptRecord);
        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(bowang);
        }
        AssertAllSelectionsConsumed(selector);
    }

    private async Task PlayAndChooseTinder(BowangParadoxScript bowang)
    {
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxArsonScriptRecord);
        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(bowang);
        }
        AssertAllSelectionsConsumed(selector);
    }
}
