using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RuthlessStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("ruthless-strike");

    [Fact]
    public async Task Exhaust_in_hand_adds_a_hit_and_upgraded_attack_ignores_block_then_resets()
    {
        var card = Upgrade(await AddToHand<RuthlessStrike>());
        var fuel = await AddCardToPile<DefendIronclad>(PileType.Hand);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), fuel);
        Assert.Equal(2, card.DynamicVars.Repeat.IntValue);
        await CreatureCmd.GainBlock(EnemyAt(0), 100, ValueProp.Unpowered, null);
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - RuthlessStrike.UpgradedDamage * 2, EnemyAt(0).CurrentHp);
        Assert.Equal(100, EnemyAt(0).Block);
        Assert.Equal(RuthlessStrike.BaseHits, card.DynamicVars.Repeat.IntValue);
    }

    [Fact]
    public async Task Another_players_exhaust_does_not_charge_the_card()
    {
        var ally = Players.Single(player => player != Player);
        var card = await AddToHand<RuthlessStrike>();
        var allyCard = await AddCardToPile<DefendIronclad>(PileType.Hand, owner: ally);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), allyCard);

        Assert.Equal(RuthlessStrike.BaseHits, card.DynamicVars.Repeat.IntValue);
    }
}
