using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BlitzkriegTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("blitzkrieg");

    [Fact]
    public async Task Plays_basic_strikes_from_hand_and_draw_but_ignores_non_strikes()
    {
        var handStrike = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var drawStrike = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var ignored = await AddCardToPile<DefendIronclad>(PileType.Draw);
        CardModel[] allStrikes = PileType.Hand.GetPile(Player).Cards
            .Concat(PileType.Draw.GetPile(Player).Cards)
            .Where(card => card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike))
            .ToArray();
        int hp = EnemyAt(0).CurrentHp;

        await Play(Upgrade(await AddToHand<Blitzkrieg>()), EnemyAt(0));

        Assert.Equal(hp - Blitzkrieg.UpgradedDamage - 6 * allStrikes.Length, EnemyAt(0).CurrentHp);
        Assert.All(allStrikes, strike => AssertInPile(strike, PileType.Discard));
        AssertInPile(handStrike, PileType.Discard);
        AssertInPile(drawStrike, PileType.Discard);
        AssertInPile(ignored, PileType.Draw);
    }

    [Fact]
    public async Task A_distinct_blitzkrieg_can_recurse_without_replaying_the_same_instance_forever()
    {
        await CardPileCmd.Add(PileType.Hand.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        var nested = await AddCardToPile<Blitzkrieg>(PileType.Draw);
        var strike = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var root = await AddToHand<Blitzkrieg>();
        int hp = EnemyAt(0).CurrentHp;

        await Play(root, EnemyAt(0));

        Assert.Equal(hp - Blitzkrieg.BaseDamage * 2 - 6, EnemyAt(0).CurrentHp);
        AssertInPile(root, PileType.Discard);
        AssertInPile(nested, PileType.Discard);
        AssertInPile(strike, PileType.Discard);
    }
}
