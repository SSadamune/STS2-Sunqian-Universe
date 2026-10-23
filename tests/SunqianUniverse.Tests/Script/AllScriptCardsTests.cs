using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class AllScriptCardsTests : SunqianCombatTestSuite
{
    private sealed record ScriptCase(
        Type CardType,
        Type RecordType,
        Type PowerType,
        bool CanBeReinforced);

    private static readonly ScriptCase[] Cases =
    [
        new(typeof(BombardChibiScript), typeof(BombardChibiScriptRecord), typeof(ScriptBombardChibiPower), false),
        new(typeof(BowangParadoxScript), typeof(BowangParadoxScriptRecord), typeof(ScriptBowangParadoxPower), false),
        new(typeof(BurningLinkedCampsScript), typeof(BurningLinkedCampsScriptRecord), typeof(ScriptBurningLinkedCampsPower), false),
        new(typeof(ChickenFootCheeseScript), typeof(ChickenFootCheeseScriptRecord), typeof(ScriptChickenFootCheesePower), false),
        new(typeof(EunuchScript), typeof(EunuchScriptRecord), typeof(ScriptEunuchPower), false),
        new(typeof(ExecutionCommanderScript), typeof(ExecutionCommanderScriptRecord), typeof(ScriptExecutionCommanderPower), true),
        new(typeof(HeadsmanScript), typeof(HeadsmanScriptRecord), typeof(ScriptHeadsmanPower), false),
        new(typeof(NeutralAmbushScript), typeof(NeutralAmbushScriptRecord), typeof(ScriptNeutralAmbushPower), false),
        new(typeof(NightRaidWuchaoScript), typeof(NightRaidWuchaoScriptRecord), typeof(ScriptNightRaidWuchaoPower), true),
        new(typeof(OneManOneCity), typeof(OneManOneCityScriptRecord), typeof(ScriptOneManOneCityPower), false),
        new(typeof(PartTimeGeneralScript), typeof(PartTimeGeneralScriptRecord), typeof(ScriptPartTimeGeneralPower), false),
        new(typeof(ServantScript), typeof(ServantScriptRecord), typeof(ScriptServantPower), false),
        new(typeof(SunqianScript), typeof(SunqianScriptRecord), typeof(ScriptSunqianPower), false),
        new(typeof(TwoWordPoetScript), typeof(TwoWordPoetScriptRecord), typeof(ScriptTwoWordPoetPower), true),
        new(typeof(XiliangSavageScript), typeof(XiliangSavageScriptRecord), typeof(ScriptXiliangSavagePower), false),
    ];

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("all-script-card-contracts");

    [Fact]
    public void Test_catalog_contains_every_registered_script_card()
    {
        Type[] expected = Cases.Select(testCase => testCase.CardType)
            .OrderBy(type => type.FullName)
            .ToArray();
        Type[] registered = ModelDb.CardPool<SunqianCardPool>().AllCards
            .OfType<ScriptCardTemplate>()
            .Select(card => card.GetType())
            .OrderBy(type => type.FullName)
            .ToArray();

        Assert.Equal(expected, registered);
    }

    [Theory]
    [InlineData(typeof(BombardChibiScript), typeof(BombardChibiScriptRecord), typeof(ScriptBombardChibiPower), false)]
    [InlineData(typeof(BowangParadoxScript), typeof(BowangParadoxScriptRecord), typeof(ScriptBowangParadoxPower), false)]
    [InlineData(typeof(BurningLinkedCampsScript), typeof(BurningLinkedCampsScriptRecord), typeof(ScriptBurningLinkedCampsPower), false)]
    [InlineData(typeof(ChickenFootCheeseScript), typeof(ChickenFootCheeseScriptRecord), typeof(ScriptChickenFootCheesePower), false)]
    [InlineData(typeof(EunuchScript), typeof(EunuchScriptRecord), typeof(ScriptEunuchPower), false)]
    [InlineData(typeof(ExecutionCommanderScript), typeof(ExecutionCommanderScriptRecord), typeof(ScriptExecutionCommanderPower), true)]
    [InlineData(typeof(HeadsmanScript), typeof(HeadsmanScriptRecord), typeof(ScriptHeadsmanPower), false)]
    [InlineData(typeof(NeutralAmbushScript), typeof(NeutralAmbushScriptRecord), typeof(ScriptNeutralAmbushPower), false)]
    [InlineData(typeof(NightRaidWuchaoScript), typeof(NightRaidWuchaoScriptRecord), typeof(ScriptNightRaidWuchaoPower), true)]
    [InlineData(typeof(OneManOneCity), typeof(OneManOneCityScriptRecord), typeof(ScriptOneManOneCityPower), false)]
    [InlineData(typeof(PartTimeGeneralScript), typeof(PartTimeGeneralScriptRecord), typeof(ScriptPartTimeGeneralPower), false)]
    [InlineData(typeof(ServantScript), typeof(ServantScriptRecord), typeof(ScriptServantPower), false)]
    [InlineData(typeof(SunqianScript), typeof(SunqianScriptRecord), typeof(ScriptSunqianPower), false)]
    [InlineData(typeof(TwoWordPoetScript), typeof(TwoWordPoetScriptRecord), typeof(ScriptTwoWordPoetPower), true)]
    [InlineData(typeof(XiliangSavageScript), typeof(XiliangSavageScriptRecord), typeof(ScriptXiliangSavagePower), false)]
    public async Task Playing_script_unlocks_matching_record_and_activates_matching_power(
        Type cardType,
        Type recordType,
        Type powerType,
        bool canBeReinforced)
    {
        CardModel card = await AddToHand(ModelDb.GetId(cardType));
        Assert.IsAssignableFrom<ScriptCardTemplate>(card);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);

        await PlayerCmd.SetEnergy(card is PartTimeGeneralScript ? 0 : 10, Player);
        if (card.TargetType == TargetType.AnyEnemy)
        {
            await Play(card, EnemyAt(0));
        }
        else
        {
            await Play(card);
        }

        ScriptRecordCardTemplate record = Assert.IsAssignableFrom<ScriptRecordCardTemplate>(
            Assert.Single(
                Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards,
                candidate => candidate.GetType() == recordType));
        Assert.Equal(powerType, record.PowerType);
        Assert.Equal(canBeReinforced, record.CanBeReinforced);
        Assert.False(record.SourceUpgraded);
        Assert.Equal(CardType.Skill, record.Type);
        Assert.False(record.ShouldShowInCardLibrary);
        Assert.DoesNotContain(CardKeyword.Unplayable, record.Keywords);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), record.VisualCardPool);
        Assert.True(record.ShouldGlowGold);
        Assert.NotEmpty(record.GetDescriptionForPile(record.Pile!.Type));

        ScriptPowerTemplate active = Assert.Single(
            Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        Assert.Equal(powerType, active.GetType());
    }
}
