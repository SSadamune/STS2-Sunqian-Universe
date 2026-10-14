using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MusouAttackTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("musou-attack");

    [Fact]
    public async Task Upgrade_adds_four_times_dexterity_to_all_enemy_damage_and_retains_the_bonus()
    {
        await ApplyPower<DexterityPower>(Player.Creature, 2);
        var card = Upgrade(await AddToHand<MusouAttack>());
        int firstHp = EnemyAt(0).CurrentHp;
        int secondHp = EnemyAt(1).CurrentHp;

        await Play(card);

        int expectedDamage = MusouAttack.UpgradedDamage + 2 * MusouAttack.UpgradedDexterityMultiplier;
        Assert.Equal(firstHp - expectedDamage, EnemyAt(0).CurrentHp);
        Assert.Equal(secondHp - expectedDamage, EnemyAt(1).CurrentHp);
        Assert.Equal(expectedDamage, card.DynamicVars.Damage.IntValue);
    }

    [Fact]
    public async Task Negative_dexterity_reduces_this_attack_but_is_not_permanently_retained()
    {
        await ApplyPower<DexterityPower>(Player.Creature, 1);
        await ApplyPower<DexterityPower>(Player.Creature, -3);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, -2);
        var card = await AddToHand<MusouAttack>();
        int hp = EnemyAt(0).CurrentHp;

        await Play(card);

        int expectedDamage = MusouAttack.CanonicalDamage
            - 2 * MusouAttack.CanonicalDexterityMultiplier;
        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        Assert.Equal(MusouAttack.CanonicalDamage, card.DynamicVars.Damage.IntValue);
    }
}
