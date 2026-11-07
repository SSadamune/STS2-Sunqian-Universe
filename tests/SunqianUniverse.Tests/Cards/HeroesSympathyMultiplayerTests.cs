using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HeroesSympathyMultiplayerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("heroes-sympathy-multiplayer");

    [Fact]
    public async Task Another_players_attack_does_not_trigger_the_owners_power()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        var remoteAttack = await AddToHand<StrikeLiubei>(Players[1]);
        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), remoteAttack, EnemyAt(0));

        Assert.Equal(0, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<HeroesSympathyPower>());
    }
}
