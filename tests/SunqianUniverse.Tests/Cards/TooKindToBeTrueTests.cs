using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TooKindToBeTrueTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("too-kind-to-be-true");

    [Fact]
    public async Task Distinct_debuff_types_each_add_upgraded_block_to_defend_cards()
    {
        var card = Upgrade(await AddToHand<TooKindToBeTrue>());
        await ApplyPower<TooKindToBeTruePower>(
            Player.Creature,
            TooKindToBeTrue.UpgradedExtraBlockPerDebuff,
            Player.Creature,
            card);
        await ApplyPower<WeakPower>(Player.Creature, 1);
        await ApplyPower<VulnerablePower>(Player.Creature, 1);

        await Play(await AddToHand<DefendLongtao>());

        Assert.Equal(DefendLongtao.BaseBlock + TooKindToBeTrue.UpgradedExtraBlockPerDebuff * 2, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<TooKindToBeTruePower>(Player.Creature, TooKindToBeTrue.UpgradedExtraBlockPerDebuff);
    }
}
