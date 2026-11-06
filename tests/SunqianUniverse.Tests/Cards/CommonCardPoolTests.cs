using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CommonCardPoolTests : SunqianCombatTestSuite
{
    private static readonly Type[] CoveredCommonCardTypes =
    [
        typeof(Audition),
        typeof(ChickenFootCheeseScript),
        typeof(Circus),
        typeof(Denied),
        typeof(EunuchScript),
        typeof(ExecutionCommanderScript),
        typeof(FingerSnapStrike),
        typeof(FireNova),
        typeof(HateTheHeavensStrike),
        typeof(HeadsmanScript),
        typeof(LaserSwordAssassination),
        typeof(Portal),
        typeof(SendHorseWithoutHay),
        typeof(ServantScript),
        typeof(StarryNightStrike),
        typeof(VoiceChange),
        typeof(WatchFireFromShore),
        typeof(Wine),
        typeof(WorkLifeBalance),
        typeof(WorryWithoutWorry),
    ];

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<SunqianCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-common-card-pool");
    }

    [Fact]
    public void Every_registered_common_card_has_an_explicit_test_entry()
    {
        string[] actual = Player.Character.CardPool
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Common)
            .Select(card => card.GetType().FullName!)
            .Order()
            .ToArray();
        string[] expected = CoveredCommonCardTypes
            .Select(type => type.FullName!)
            .Order()
            .ToArray();

        Assert.Equal(expected, actual);
    }

}
