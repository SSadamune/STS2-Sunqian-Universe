using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class OpenDefecationTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("open-defecation");

    [Fact]
    public async Task Discards_unplayable_cards_from_hand_and_draw_then_grants_dexterity()
    {
        var handWound = await AddToHand<Wound>();
        var drawWound = await AddToDrawPileTop<Wound>();

        await Play(Upgrade(await AddToHand<OpenDefecation>()));

        AssertInPile(handWound, PileType.Discard);
        AssertInPile(drawWound, PileType.Discard);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(Player.Creature, OpenDefecation.UpgradedDexterity);
    }
}
