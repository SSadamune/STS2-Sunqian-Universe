using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class EunuchScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-eunuch-script");
    }

    [Fact]
    public async Task EU_01_replaying_active_eunuch_keeps_power_and_all_marks()
    {
        var oldMarkedSecond = await AddToDrawPileTop<DefendLongtao>();
        var oldMarkedFirst = await AddToDrawPileTop<FlameStrike>();
        var firstEunuch = await AddToHand<EunuchScript>();

        await PlayAndActivateScript(firstEunuch);

        var oldPower = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());
        AssertMarkedInHand(oldMarkedFirst);
        AssertMarkedInHand(oldMarkedSecond);

        var newMarkedSecond = await AddToDrawPileTop<BlockFirefighting>();
        var newMarkedFirst = await AddToDrawPileTop<FingerSnapStrike>();
        var secondEunuch = await AddToHand<EunuchScript>();

        await PlayAndActivateScript(secondEunuch);

        var newPower = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());
        Assert.Same(oldPower, newPower);
        AssertOnlyScript<ScriptEunuchPower>();

        AssertMarkedInHand(oldMarkedFirst);
        AssertMarkedInHand(oldMarkedSecond);
        AssertMarkedInHand(newMarkedFirst);
        AssertMarkedInHand(newMarkedSecond);
    }

    [Fact]
    public async Task EU_02_card_that_leaves_hand_loses_message_and_is_not_processed_again()
    {
        var stillTracked = await AddToDrawPileTop<FlameStrike>();
        var playedCard = await AddToDrawPileTop<DefendLongtao>();
        var eunuch = await AddToHand<EunuchScript>();

        await PlayAndActivateScript(eunuch);

        AssertMarkedInHand(playedCard);
        AssertMarkedInHand(stillTracked);

        await Play(playedCard);

        AssertInPile(playedCard, PileType.Discard);
        Assert.False(playedCard.HasEunuchMessage());

        var replacement = await AddToHand<BowangParadoxScript>();
        await PlayAndActivateScript(replacement);

        AssertInPile(playedCard, PileType.Discard);
        Assert.False(playedCard.HasEunuchMessage());
        AssertInPile(stillTracked, PileType.Exhaust);
        Assert.False(stillTracked.HasEunuchMessage());
        AssertOnlyScript<ScriptBowangParadoxPower>();
    }

    private static void AssertMarkedInHand(CardModel card)
    {
        AssertInPile(card, PileType.Hand);
        Assert.True(card.HasEunuchMessage());
        Assert.True(card.ShouldRetainThisTurn);
    }
}
