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

        await Play(bowang);

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
    public async Task BP_02_single_turn_retain_prevents_play_time_discard()
    {
        var retainedAttack = GiveSingleTurnRetain(await AddToHand<FlameStrike>());
        var bowang = await AddToHand<BowangParadoxScript>();

        Assert.True(retainedAttack.ShouldRetainThisTurn);
        await Play(bowang);

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
        await Play(bowang);

        AssertInPile(attack, PileType.Discard);
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    [Fact]
    public async Task BP_04_replacement_discards_only_unretained_burning_cards()
    {
        var bowang = await AddToHand<BowangParadoxScript>();
        await Play(bowang);

        var burningAttack = await AddToHand<FlameStrike>();
        var burningSkill = await AddToHand<BlockFirefighting>();
        var retainedBurning = GiveSingleTurnRetain(await AddToHand<FlameStrike>());
        var nonBurningSkill = await AddToHand<DefendLongtao>();
        var replacement = await AddToHand<ServantScript>();

        await Play(replacement);

        AssertInPile(burningAttack, PileType.Discard);
        AssertInPile(burningSkill, PileType.Discard);
        AssertInPile(retainedBurning, PileType.Hand);
        AssertInPile(nonBurningSkill, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task BP_05_eunuch_protects_newly_drawn_burning_cards_from_old_bowang()
    {
        var bowang = await AddToHand<BowangParadoxScript>();
        await Play(bowang);

        var preexistingBurning = await AddToHand<FlameStrike>();
        var drawnSecond = await AddToDrawPileTop<BlockFirefighting>();
        var drawnFirst = await AddToDrawPileTop<FlameStrike>();
        var eunuch = await AddToHand<EunuchScript>();

        await Play(eunuch);

        AssertInPile(preexistingBurning, PileType.Discard);
        AssertEunuchProtectedInHand(drawnFirst);
        AssertEunuchProtectedInHand(drawnSecond);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptEunuchPower>();
    }

    [Fact]
    public async Task BP_06_upgraded_eunuch_protects_all_three_drawn_burning_cards()
    {
        var bowang = await AddToHand<BowangParadoxScript>();
        await Play(bowang);

        var drawnThird = await AddToDrawPileTop<FlameStrike>();
        var drawnSecond = await AddToDrawPileTop<BlockFirefighting>();
        var drawnFirst = await AddToDrawPileTop<FlameStrike>();
        var eunuch = Upgrade(await AddToHand<EunuchScript>());

        Assert.True(eunuch.IsUpgraded);
        await Play(eunuch);

        AssertEunuchProtectedInHand(drawnFirst);
        AssertEunuchProtectedInHand(drawnSecond);
        AssertEunuchProtectedInHand(drawnThird);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptEunuchPower>();
    }

    private static void AssertEunuchProtectedInHand(CardModel card)
    {
        AssertInPile(card, PileType.Hand);
        Assert.True(card.HasEunuchMessage());
        Assert.True(card.ShouldRetainThisTurn);
    }
}
