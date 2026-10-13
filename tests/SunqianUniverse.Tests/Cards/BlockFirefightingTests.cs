using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BlockFirefightingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("block-firefighting");

    [Fact]
    public async Task Cannot_be_played_when_no_enemy_is_burning()
    {
        var card = await AddToHand<BlockFirefighting>();

        bool canPlay = card.CanPlay(out UnplayableReason reason, out _);

        Assert.False(canPlay);
        Assert.NotEqual(UnplayableReason.None, reason);
    }

    [Fact]
    public async Task FlameStrike_enables_upgraded_card_and_effects_stack()
    {
        var enemy = EnemyAt(0);
        await Play(await AddToHand<FlameStrike>(), enemy);
        var card = Upgrade(await AddToHand<BlockFirefighting>());

        await Play(card, enemy);

        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, FlameStrike.BaseBurning * 2 + BlockFirefighting.UpgradedBurning);
        CardTestAssertions.AssertPowerAmount<UnextinguishedPower>(enemy, BlockFirefighting.UpgradedUnextinguished);
        CardTestAssertions.AssertPowerAmount<WeakPower>(Player.Creature, BlockFirefighting.WeakAmount);
    }
}
