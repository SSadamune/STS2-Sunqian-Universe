using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class IronChainBoatsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("iron-chain-boats");

    [Fact]
    public async Task Burning_damage_on_chained_enemy_is_copied_to_every_enemy()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        var card = Upgrade(await AddToHand<IronChainBoats>());
        await Play(card, first);
        var chain = Assert.IsType<IronChainPower>(first.GetPower<IronChainPower>());
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        await chain.AfterBurningDamage(5);

        Assert.Equal(firstHp - 5, first.CurrentHp);
        Assert.Equal(secondHp - 5, second.CurrentHp);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
    }
}
