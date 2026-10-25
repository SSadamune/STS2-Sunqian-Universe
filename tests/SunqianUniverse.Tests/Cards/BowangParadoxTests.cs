using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BowangParadoxTests : SunqianCombatTestSuite
{
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
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    [Fact]
    public async Task BP_07_upgraded_vigor_choice_grants_nine_vigor()
    {
        var bowang = Upgrade(await AddToHand<BowangParadoxScript>());

        await PlayAndChooseVigor(bowang);

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            BowangParadoxScript.UpgradedVigor);
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    [Fact]
    public async Task BP_08_upgraded_tinder_choice_grants_nine_tinder()
    {
        var bowang = Upgrade(await AddToHand<BowangParadoxScript>());
        await PlayAndChooseTinder(bowang);

        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.UpgradedTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    [Fact]
    public async Task BP_02_single_turn_retain_prevents_play_time_discard()
    {
        var retainedAttack = GiveSingleTurnRetain(await AddToHand<FlameStrike>());
        var bowang = await AddToHand<BowangParadoxScript>();

        Assert.True(retainedAttack.ShouldRetainThisTurn);
        await PlayAndChooseVigor(bowang);

        AssertInPile(retainedAttack, PileType.Hand);
        AssertOnlyScript<ScriptBowangParadoxPower>();
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
        AssertOnlyScript<ScriptBowangParadoxPower>();
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
        AssertOnlyScript<ScriptBowangParadoxPower>();
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
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    private async Task PlayAndChooseVigor(BowangParadoxScript bowang)
    {
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxVigorChoice);
        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(bowang);
        }
        AssertAllSelectionsConsumed(selector);
    }

    private async Task PlayAndChooseTinder(BowangParadoxScript bowang)
    {
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxTinderChoice);
        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(bowang);
        }
        AssertAllSelectionsConsumed(selector);
    }
}
