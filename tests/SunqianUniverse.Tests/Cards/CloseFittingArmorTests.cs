using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CloseFittingArmorTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("close-fitting-armor");

    [Fact]
    public async Task Dexterity_is_added_to_plating_and_upgrade_adds_retain()
    {
        await ApplyPower<DexterityPower>(Player.Creature, 2);
        var card = Upgrade(await AddToHand<CloseFittingArmor>());

        await Play(card);

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            CloseFittingArmor.BasePlating + 2);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
    }

    [Fact]
    public async Task Negative_dexterity_cannot_make_plating_negative()
    {
        await ApplyPower<DexterityPower>(Player.Creature, 1);
        await ApplyPower<DexterityPower>(Player.Creature, -11);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, -10);

        await Play(await AddToHand<CloseFittingArmor>());

        Assert.Null(Player.Creature.GetPower<PlatingPower>());
    }
}
