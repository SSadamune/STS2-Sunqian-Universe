using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WhyDoubtWeiTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("why-doubt-wei");

    [Fact]
    public async Task Ethereal_exhaust_grants_block_and_weak_instead_of_attack_rewards()
    {
        var card = Upgrade(await AddToHand<WhyDoubtWei>());

        await CardCmd.Exhaust(
            new BlockingPlayerChoiceContext(),
            card,
            causedByEthereal: true);

        AssertInPile(card, PileType.Exhaust);
        Assert.Equal(WhyDoubtWei.UpgradedBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<WeakPower>(Player.Creature, WhyDoubtWei.UpgradedWeak);
        Assert.Null(Player.Creature.GetPower<StrengthPower>());
    }

    [Fact]
    public async Task Playing_upgraded_card_uses_attack_branch_and_grants_strength()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<WhyDoubtWei>()), enemy);

        Assert.Equal(hp - WhyDoubtWei.UpgradedDamage, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(Player.Creature, WhyDoubtWei.UpgradedStrength);
    }
}
