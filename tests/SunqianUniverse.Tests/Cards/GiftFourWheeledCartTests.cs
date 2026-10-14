using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GiftFourWheeledCartTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("gift-four-wheeled-cart");

    [Fact]
    public async Task Retired_card_still_grants_one_random_living_creature_a_cart_and_its_stats()
    {
        var card = Upgrade(await AddToHand<GiftFourWheeledCart>());

        await Play(card);

        var creatures = new[] { Player.Creature, EnemyAt(0) };
        var recipient = Assert.Single(creatures.Where(creature =>
            creature.HasPower<FourWheeledCartPower>()));
        CardTestAssertions.AssertPowerAmount<FourWheeledCartPower>(recipient, 1);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(recipient, GiftFourWheeledCart.StrengthAmount);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(recipient, GiftFourWheeledCart.DexterityAmount);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
    }
}
