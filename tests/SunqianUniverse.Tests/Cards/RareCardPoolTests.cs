using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RareCardPoolTests : SunqianCombatTestSuite
{
    private static readonly Type[] CoveredRareCardTypes =
    [
        typeof(AllIn),
        typeof(BenevolenceRighteousnessDualSwords),
        typeof(Blitzkrieg),
        typeof(BombardChibiScript),
        typeof(BurningLinkedCampsScript),
        typeof(CelebrateMourning),
        typeof(CloseFittingArmor),
        typeof(CrossTheRiver),
        typeof(DistributedScreenwriter),
        typeof(GoldenUprising),
        typeof(GrandGoblet),
        typeof(GreatestPassOfCentralPlains),
        typeof(GuanDiForm),
        typeof(HuaguMianzhang),
        typeof(HumanTransmutation),
        typeof(IronChainBoats),
        typeof(LittleThirdBigFourth),
        typeof(LoudSecretPlot),
        typeof(LoyaltyOverKin),
        typeof(Metamorphosis),
        typeof(MonsterHuntingMethod),
        typeof(MusouAttack),
        typeof(PartTimeGeneralScript),
        typeof(RuthlessStrike),
        typeof(SelfDecapitationAscension),
        typeof(SharpenSwordForDecades),
        typeof(WaterFireInvincible),
    ];

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddRemotePlayer<SunqianCharacter>()
            .AddEnemy<BigDummy>().WithSeed("sunqian-rare-card-pool");

    [Fact]
    public void Every_registered_rare_card_has_an_explicit_test_entry()
    {
        string[] actual = Player.Character.CardPool
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Rare)
            .Select(card => card.GetType().FullName!)
            .Order()
            .ToArray();
        string[] expected = CoveredRareCardTypes.Select(type => type.FullName!).Order().ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Retired_rare_cards_are_still_covered_separately()
    {
        Assert.Equal(CardRarity.Rare, Combat.CreateCard<GiftFourWheeledCart>(Player).Rarity);
        Assert.Equal(CardRarity.Rare, Combat.CreateCard<MutatedCentaur>(Player).Rarity);
        Assert.Equal(CardRarity.Rare, Combat.CreateCard<NeverHadThese>(Player).Rarity);
    }
}
