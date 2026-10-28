using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
        new(typeof(BowangParadoxScript), typeof(BowangParadoxAdvanceScriptRecord), typeof(ScriptBowangParadoxAdvancePower), false),
        new(typeof(GuanDiForm), typeof(GuanYuCivilVer), typeof(GuanYuCivilVerPower), false),
        new(typeof(BurningLinkedCampsScript), typeof(BurningLinkedCampsScriptRecord), typeof(ScriptBurningLinkedCampsPower), false),
        new(typeof(ChickenFootCheeseScript), typeof(ChickenFootCheeseScriptRecord), typeof(ScriptChickenFootCheesePower), false),
        new(typeof(EunuchScript), typeof(EunuchScriptRecord), typeof(ScriptEunuchPower), false),
        new(typeof(ExecutionCommanderScript), typeof(ExecutionCommanderScriptRecord), typeof(ScriptExecutionCommanderPower), true),
        new(typeof(HeadsmanScript), typeof(HeadsmanScriptRecord), typeof(ScriptHeadsmanPower), false),
        new(typeof(NeutralAmbushScript), typeof(NeutralAmbushScriptRecord), typeof(ScriptNeutralAmbushPower), false),
        new(typeof(NightRaidWuchaoScript), typeof(NightRaidWuchaoScriptRecord), typeof(ScriptNightRaidWuchaoPower), true),
        new(typeof(OneManOneCity), typeof(OneManOneCityScriptRecord), typeof(ScriptOneManOneCityPower), false),
        new(typeof(PartTimeGeneralScript), typeof(PartTimeGeneralScriptRecord), typeof(ScriptPartTimeGeneralPower), true),
        new(typeof(GruntScript), typeof(GruntScriptRecord), typeof(ScriptGruntPower), false),
        new(typeof(ServantScript), typeof(ServantScriptRecord), typeof(ScriptServantPower), false),
        new(typeof(SunqianScript), typeof(SunqianScriptRecord), typeof(ScriptSunqianPower), false),
        new(typeof(TwoWordPoetScript), typeof(TwoWordPoetScriptRecord), typeof(ScriptTwoWordPoetPower), false),
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

    [Fact]
    public void Canonical_script_cards_can_render_in_the_card_library()
    {
        foreach (ScriptCase testCase in Cases)
        {
            CardModel canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(testCase.CardType));

            Assert.False(canonical.IsMutable);
            Assert.NotEmpty(canonical.GetDescriptionForPile(PileType.Deck));
            Assert.NotEmpty(canonical.HoverTips.OfType<CardHoverTip>());
            CardModel record = ModelDb.GetById<CardModel>(ModelDb.GetId(testCase.RecordType));
            Assert.DoesNotContain("剧本：", record.Title);
            Assert.DoesNotContain("Script:", record.Title);
        }
    }

    [Fact]
    public void Script_record_descriptions_only_color_the_fixed_civil_multiplier_blue()
    {
        foreach (ScriptCase testCase in Cases)
        {
            CardModel record = ModelDb.GetById<CardModel>(ModelDb.GetId(testCase.RecordType));

            string description = record.GetDescriptionForPile(PileType.Deck);
            if (record is GuanYuCivilVer)
            {
                Assert.Contains("[blue]2[/blue]", description);
            }
            else
            {
                Assert.DoesNotContain("[blue]", description);
            }
        }
    }

    [Fact]
    public void All_script_activation_descriptions_use_this_script_and_preserve_the_expected_keyword_rows()
    {
        const string activation = "激活此[gold]剧本[/gold]";
        const string exhaust = "[gold]消耗[/gold]。";
        foreach (ScriptCase testCase in Cases)
        {
            var canonical = (ScriptCardTemplate)ModelDb.GetById<CardModel>(ModelDb.GetId(testCase.CardType));
            AssertDescription(canonical.GetDescriptionForPile(PileType.Deck), canonical);
            var upgraded = (ScriptCardTemplate)canonical.ToMutable();
            upgraded.UpgradeInternal();
            AssertDescription(upgraded.GetDescriptionForPile(PileType.Deck), upgraded);
            AssertDescription(upgraded.GetDescriptionForUpgradePreview(), upgraded);

            void AssertDescription(string description, CardModel card)
            {
                if (card is GuanDiForm)
                {
                    Assert.Contains("将剧本：[gold]文关羽[/gold]和剧本：[gold]武关羽[/gold]加入剧本集。", description);
                    return;
                }
                if (card is BowangParadoxScript)
                {
                    Assert.Contains("从两种剧本中选择一种激活，另一种也会加入剧本集。", description);
                    Assert.DoesNotContain("数值叠加", description);
                    Assert.Contains("\n" + exhaust, description);
                    return;
                }
                Assert.Contains(activation, description);
                Assert.DoesNotContain("激活[gold]剧本：", description);
                Assert.DoesNotContain("[gold]激活", description);
                string activationLine = Assert.Single(description.Split('\n')
                    .Where(line => line.Contains(activation)));
                if (!card.Keywords.Contains(CardKeyword.Exhaust))
                {
                    Assert.DoesNotContain(exhaust, description);
                }
                else if (testCase.CanBeReinforced && card is not PartTimeGeneralScript)
                {
                    Assert.DoesNotContain(exhaust, activationLine);
                    Assert.Contains("\n" + exhaust, description);
                }
                else
                {
                    Assert.Contains(activation + "。" + exhaust, activationLine);
                }

                if (testCase.CanBeReinforced && testCase.CardType != typeof(PartTimeGeneralScript))
                {
                    Assert.Contains(activation + "，若激活过则还会将数值叠加。", description);
                }
                else
                {
                    Assert.DoesNotContain("若激活过则还会将数值叠加", description);
                }
            }
        }
    }

    [Fact]
    public async Task Various_strike_cards_text_has_the_registered_hover_tip_everywhere()
    {
        const string marker = "[gold]各种打击牌[/gold]";
        CardModel[] cards =
        [
            ModelDb.Card<BenevolenceRighteousnessDualSwords>(),
            ModelDb.Card<PartTimeGeneralScript>(),
            ModelDb.Card<GruntScriptRecord>(),
            ModelDb.Card<NightRaidWuchaoScriptRecord>(),
        ];
        foreach (CardModel card in cards)
        {
            Assert.Contains(marker, card.GetDescriptionForPile(PileType.Deck));
        }

        HoverTip expectedTip = Assert.IsType<HoverTip>(
            HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards));
        Assert.Equal("各种打击牌", expectedTip.Title);
        Assert.Equal(
            "牌名中包含「打击」的攻击牌，均被称为[gold]各种打击牌[/gold]。",
            expectedTip.Description);

        var choiceContext = new BlockingPlayerChoiceContext();
        PowerModel[] powers =
        [
            Assert.IsType<ScriptGruntPower>(await PowerCmd.Apply<ScriptGruntPower>(
                choiceContext, Player.Creature, 1m, Player.Creature, null)),
            Assert.IsType<ScriptViceGeneralPower>(await PowerCmd.Apply<ScriptViceGeneralPower>(
                choiceContext, Player.Creature, 1m, Player.Creature, null)),
            Assert.IsType<ScriptNightRaidWuchaoPower>(await PowerCmd.Apply<ScriptNightRaidWuchaoPower>(
                choiceContext, Player.Creature, 1m, Player.Creature, null)),
            Assert.IsType<BenevolenceRighteousnessDualSwordsPower>(
                await PowerCmd.Apply<BenevolenceRighteousnessDualSwordsPower>(
                    choiceContext, Player.Creature, 1m, Player.Creature, null)),
        ];
        IEnumerable<IHoverTip>[] hoverTipSets =
        [
            ..cards.Select(card => card.HoverTips),
            ..powers.Select(power => power.HoverTips),
        ];
        foreach (IEnumerable<IHoverTip> hoverTips in hoverTipSets)
        {
            Assert.Contains(hoverTips, tip => tip.Id == expectedTip.Id);
        }

        HoverTip defendTip = Assert.IsType<HoverTip>(
            HoverTipFactory.FromKeyword(SquKeywords.VariousDefendCards));
        Assert.Equal("各种防御牌", defendTip.Title);
        Assert.Contains(
            powers.OfType<ScriptViceGeneralPower>().Single().HoverTips,
            tip => tip.Id == defendTip.Id);
    }

    [Theory]
    [InlineData(typeof(BombardChibiScript), typeof(BombardChibiScriptRecord), typeof(ScriptBombardChibiPower), false)]
    [InlineData(typeof(BowangParadoxScript), typeof(BowangParadoxAdvanceScriptRecord), typeof(ScriptBowangParadoxAdvancePower), false)]
    [InlineData(typeof(GuanDiForm), typeof(GuanYuCivilVer), typeof(GuanYuCivilVerPower), false)]
    [InlineData(typeof(BurningLinkedCampsScript), typeof(BurningLinkedCampsScriptRecord), typeof(ScriptBurningLinkedCampsPower), false)]
    [InlineData(typeof(ChickenFootCheeseScript), typeof(ChickenFootCheeseScriptRecord), typeof(ScriptChickenFootCheesePower), false)]
    [InlineData(typeof(EunuchScript), typeof(EunuchScriptRecord), typeof(ScriptEunuchPower), false)]
    [InlineData(typeof(ExecutionCommanderScript), typeof(ExecutionCommanderScriptRecord), typeof(ScriptExecutionCommanderPower), true)]
    [InlineData(typeof(HeadsmanScript), typeof(HeadsmanScriptRecord), typeof(ScriptHeadsmanPower), false)]
    [InlineData(typeof(NeutralAmbushScript), typeof(NeutralAmbushScriptRecord), typeof(ScriptNeutralAmbushPower), false)]
    [InlineData(typeof(NightRaidWuchaoScript), typeof(NightRaidWuchaoScriptRecord), typeof(ScriptNightRaidWuchaoPower), true)]
    [InlineData(typeof(OneManOneCity), typeof(OneManOneCityScriptRecord), typeof(ScriptOneManOneCityPower), false)]
    [InlineData(typeof(PartTimeGeneralScript), typeof(PartTimeGeneralScriptRecord), typeof(ScriptPartTimeGeneralPower), true)]
    [InlineData(typeof(GruntScript), typeof(GruntScriptRecord), typeof(ScriptGruntPower), false)]
    [InlineData(typeof(ServantScript), typeof(ServantScriptRecord), typeof(ScriptServantPower), false)]
    [InlineData(typeof(SunqianScript), typeof(SunqianScriptRecord), typeof(ScriptSunqianPower), false)]
    [InlineData(typeof(TwoWordPoetScript), typeof(TwoWordPoetScriptRecord), typeof(ScriptTwoWordPoetPower), false)]
    [InlineData(typeof(XiliangSavageScript), typeof(XiliangSavageScriptRecord), typeof(ScriptXiliangSavagePower), false)]
    public async Task Playing_script_unlocks_matching_record_and_activates_matching_power(
        Type cardType,
        Type recordType,
        Type powerType,
        bool canBeReinforced)
    {
        CardModel card = await AddToHand(ModelDb.GetId(cardType));
        Assert.IsAssignableFrom<ScriptCardTemplate>(card);
        if (card is OneManOneCity or GuanDiForm)
        {
            Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        }
        else
        {
            Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        }

        await PlayerCmd.SetEnergy(card is PartTimeGeneralScript ? 0 : 10, Player);
        if (card is GuanDiForm)
        {
            await Play(card);
            var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
            Assert.Single(records.OfType<GuanYuCivilVer>());
            Assert.Single(records.OfType<GuanYuMartialVer>());
            Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
            return;
        }
        if (card is BowangParadoxScript)
        {
            var selector = new ScriptedCardSelector()
                .ThenSelectWhere(candidate => candidate is BowangParadoxAdvanceScriptRecord);
            using (UseCardSelector(selector))
            {
                await Play(card);
            }
            AssertAllSelectionsConsumed(selector);
        }
        else if (card.TargetType == TargetType.AnyEnemy)
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
