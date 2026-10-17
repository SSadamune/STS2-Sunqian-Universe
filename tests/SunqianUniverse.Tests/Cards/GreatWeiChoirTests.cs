using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GreatWeiChoirTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("great-wei-choir");

    [Fact]
    public async Task Stacks_raise_the_limit_and_player_and_enemy_turns_have_separate_limits()
    {
        GreatWeiChoir first = await AddToHand<GreatWeiChoir>();
        Assert.Equal(1, first.EnergyCost.GetResolved());
        Assert.Equal(CardType.Power, first.Type);
        Assert.Equal(CardRarity.Uncommon, first.Rarity);

        await Play(first);
        await Play(await AddToHand<GreatWeiChoir>());

        CardTestAssertions.AssertPowerAmount<GreatWeiChoirPower>(Player.Creature, 2);

        await ExtinguishGuaranteed(EnemyAt(0), 2);
        await ExtinguishGuaranteed(EnemyAt(1), 3);
        await ExtinguishGuaranteed(EnemyAt(2), 7);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 5);

        ((CombatState)Player.Creature.CombatState!).CurrentSide = CombatSide.Enemy;
        await ExtinguishGuaranteed(Player.Creature, 4);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 9);
    }

    [Fact]
    public async Task Upgrade_immediately_triggers_every_enemy_burning_once()
    {
        Creature firstEnemy = EnemyAt(0);
        Creature secondEnemy = EnemyAt(1);
        await ApplyPower<BurningPower>(firstEnemy, 3);
        await ApplyPower<UnextinguishedPower>(firstEnemy, 1);
        await ApplyPower<BurningPower>(secondEnemy, 5);
        await ApplyPower<UnextinguishedPower>(secondEnemy, 1);
        int firstHpBefore = firstEnemy.CurrentHp;
        int secondHpBefore = secondEnemy.CurrentHp;

        await Play(Upgrade(await AddToHand<GreatWeiChoir>()));

        Assert.Equal(firstHpBefore - 3, firstEnemy.CurrentHp);
        Assert.Equal(secondHpBefore - 5, secondEnemy.CurrentHp);
        Assert.NotNull(firstEnemy.GetPower<BurningPower>());
        Assert.NotNull(secondEnemy.GetPower<BurningPower>());
        Assert.Null(firstEnemy.GetPower<UnextinguishedPower>());
        Assert.Null(secondEnemy.GetPower<UnextinguishedPower>());
        CardTestAssertions.AssertPowerAmount<GreatWeiChoirPower>(Player.Creature, 1);
    }

    private async Task ExtinguishGuaranteed(Creature target, int amount)
    {
        await ApplyPower<BurningPower>(target, amount);
        BurningPower burning = Assert.IsType<BurningPower>(target.GetPower<BurningPower>());
        var choiceContext = new BlockingPlayerChoiceContext();

        await burning.TriggerAsync(
            choiceContext,
            shouldCheckExtinguish: false,
            clearChanceIncrement: 1f);
        await burning.TriggerAsync(choiceContext);

        Assert.Null(target.GetPower<BurningPower>());
    }
}
