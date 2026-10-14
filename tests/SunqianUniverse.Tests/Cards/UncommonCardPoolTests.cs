using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class UncommonCardPoolTests : SunqianCombatTestSuite
{
    private static readonly Type[] CoveredUncommonCardTypes =
    [
        typeof(ArmInjuryWindmill),
        typeof(BlockFirefighting),
        typeof(BowangParadoxScript),
        typeof(BuddyHu),
        typeof(BuddyLongtaodi),
        typeof(BuddyTangxiaohu),
        typeof(BurnAfterReading),
        typeof(CatInTheBox),
        typeof(CelebrateMourning),
        typeof(CybertronStrength),
        typeof(DigRaid),
        typeof(DrinkingTableCulture),
        typeof(FlameStrike),
        typeof(JuggleMultipleShoots),
        typeof(KeepCalm),
        typeof(KeepPlayingKeepDancing),
        typeof(KillingIntentPerception),
        typeof(LensReshoot),
        typeof(NeutralAmbushScript),
        typeof(NightRaidWuchaoScript),
        typeof(NotAfraidOfAcid),
        typeof(OneManOneCity),
        typeof(OpenDefecation),
        typeof(Phasing),
        typeof(RelocateCapital),
        typeof(RespectElders),
        typeof(StandardBaseVehicle),
        typeof(Stargazing),
        typeof(TableFlip),
        typeof(TheBackupsBackup),
        typeof(ThunderOnStillLake),
        typeof(TooKindToBeTrue),
        typeof(TooSmartToBeReal),
        typeof(TransparentHole),
        typeof(TwoWordPoetScript),
        typeof(WhatToEat),
        typeof(WhyDoubtWei),
        typeof(XiliangSavageScript),
    ];

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<SunqianCharacter>()
            .AddRemotePlayer<SunqianCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-uncommon-card-pool");
    }

    [Fact]
    public void Every_registered_uncommon_card_has_an_explicit_test_entry()
    {
        string[] actual = Player.Character.CardPool
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Uncommon)
            .Select(card => card.GetType().FullName!)
            .Order()
            .ToArray();
        string[] expected = CoveredUncommonCardTypes
            .Select(type => type.FullName!)
            .Order()
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Non_character_pool_uncommons_are_covered_separately()
    {
        var arrogant = Combat.CreateCard<TheArrogantFall>(Player);
        var fireMeteor = Combat.CreateCard<FireMeteorShower>(Player);
        var grieving = Combat.CreateCard<TheGrievingPrevail>(Player);
        var knowWrong = Combat.CreateCard<KnowWrongDenyWrong>(Player);
        var saidAcid = Combat.CreateCard<SaidNotAfraidOfAcid>(Player);

        Assert.Equal(CardRarity.Uncommon, arrogant.Rarity);
        Assert.Equal(CardRarity.Uncommon, fireMeteor.Rarity);
        Assert.Equal(CardRarity.Uncommon, grieving.Rarity);
        Assert.Equal(CardRarity.Uncommon, knowWrong.Rarity);
        Assert.Equal(CardRarity.Uncommon, saidAcid.Rarity);
    }
}
