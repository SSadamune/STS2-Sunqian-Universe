using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RevengeStrikeMultiplayerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("revenge-strike-multiplayer");

    [Fact]
    public async Task Another_players_losses_do_not_enter_this_owners_window()
    {
        var other = Players.Single(player => player != Player);
        Player.Creature.SetCurrentHpInternal(Player.Creature.MaxHp);
        other.Creature.SetCurrentHpInternal(other.Creature.MaxHp);
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            other.Creature,
            6,
            ValueProp.Unblockable | ValueProp.Unpowered,
            other.Creature,
            null,
            null);

        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(6, RevengeStrikeTrackerPower.GetLostHp(other, 1));
        var card = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+0)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 7, EnemyAt(0).CurrentHp);
    }
}
