using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SelfDecapitationAscensionTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("self-decapitation");

    [Fact]
    public async Task Upgrade_kills_owner_and_gives_each_ally_intangible_energy_and_cards_from_x()
    {
        var ally = Players.Single(player => player != Player);
        for (int i = 0; i < 4; i++)
        {
            await AddCardToPile<StrikeIronclad>(PileType.Draw, owner: ally);
        }
        await PlayerCmd.SetEnergy(2, Player);
        int allyEnergy = ally.PlayerCombatState!.Energy;
        int allyHand = PileType.Hand.GetPile(ally).Cards.Count;
        ((CombatState)Player.Creature.CombatState!).CurrentSide = CombatSide.Enemy;

        await Play(Upgrade(await AddToHand<SelfDecapitationAscension>()));

        Assert.True(Player.Creature.IsDead);
        CardTestAssertions.AssertPowerAmount<IntangiblePower>(
            ally.Creature,
            SelfDecapitationAscension.UpgradedIntangibleAmount);
        Assert.Equal(allyEnergy + 3, ally.PlayerCombatState.Energy);
        Assert.Equal(allyHand + 3, PileType.Hand.GetPile(ally).Cards.Count);
    }
}
