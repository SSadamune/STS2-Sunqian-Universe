using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HoverCardUpgradePreviewTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("hover-card-upgrade-preview");

    [Fact]
    public void Every_hover_card_that_follows_its_source_upgrade_preserves_its_upgrade_preview()
    {
        AssertUpgradePreviewIsGreen<BurningLinkedCampsScript, ShangfangguSigh>();
        AssertUpgradePreviewIsGreen<ChickenFootCheeseScript, ChickenFootCheeseScriptStrikePreview>();
        AssertUpgradePreviewIsGreen<ExecutionCommanderScript, SalvoStrike>();
        AssertUpgradePreviewIsGreen<GuanDiForm, GuanYuCivilVer>();
        AssertUpgradePreviewIsGreen<GuanDiForm, GuanYuMartialVer>();
        AssertUpgradePreviewIsGreen<NightRaidWuchaoScript,
            NightRaidWuchaoScriptStarryNightStrikePreview>();
        AssertUpgradePreviewIsGreen<NeutralAmbushScript, GiantRock>();
        AssertUpgradePreviewIsGreen<NeutralAmbushScript, SalvoStrike>();
        AssertUpgradePreviewIsGreen<OpenDefecation, Splash>();
        // Wine upgrades an energy-icons argument. The icon count changes, but there is no
        // textual span for the game's upgrade renderer to color green.
        AssertUpgradePreviewIsGreen<SaidNotAfraidOfAcid, Wine>(expectDescriptionHighlight: false);
        AssertUpgradePreviewIsGreen<ServantScript, ThrowHimOut>();
        AssertUpgradePreviewIsGreen<TooKindToBeTrue, UltimateDefend>();
        AssertUpgradePreviewIsGreen<WhatToEat, ExactlyWhatToEat>();

        // Slight Revision target cards use the same path, including upgrade-only text
        // such as Burn City's "two" becoming "three" despite having no upgraded DynamicVar.
        AssertUpgradePreviewIsGreen<FireNova, FireMeteorShower>();
        AssertUpgradePreviewIsGreen<NotAfraidOfAcid, SaidNotAfraidOfAcid>();
        AssertUpgradePreviewIsGreen<RelocateCapital, BurnCity>();
    }

    [Fact]
    public void Hover_card_on_an_already_upgraded_card_is_finalized_and_not_green()
    {
        NeutralAmbushScript source = Upgrade(Combat.CreateCard<NeutralAmbushScript>(Player));
        CardModel salvo = GetHoverCard<SalvoStrike>(source);

        Assert.True(salvo.IsUpgraded);
        Assert.DoesNotContain("[green]", salvo.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public void Upgraded_script_hover_cards_highlight_every_changed_script_effect()
    {
        AssertScriptRecordUpgradePreviewIsGreen<ServantScript, ServantScriptRecord>("叉出去+");
		AssertScriptRecordUpgradePreviewIsGreen<TwoWordPoetScript, TwoWordPoetScriptRecord>("所有");
        AssertScriptRecordUpgradePreviewIsGreen<SunqianScript, SunqianScriptRecord>("3");
        AssertScriptRecordUpgradePreviewIsGreen<ChickenFootCheeseScript, ChickenFootCheeseScriptRecord>("9");
        AssertScriptRecordUpgradePreviewIsGreen<ExecutionCommanderScript, ExecutionCommanderScriptRecord>("75");
        AssertScriptRecordUpgradePreviewIsGreen<HeadsmanScript, HeadsmanScriptRecord>("4");
        AssertScriptRecordUpgradePreviewIsGreen<XiliangSavageScript, XiliangSavageScriptRecord>("5");
        AssertScriptRecordUpgradePreviewIsGreen<NightRaidWuchaoScript, NightRaidWuchaoScriptRecord>("4");
        AssertScriptRecordUpgradePreviewIsGreen<BowangParadoxScript, BowangParadoxScriptRecord>("9", "9");
        AssertScriptRecordUpgradePreviewIsGreen<GruntScript, GruntScriptRecord>("4");
    }

    [Fact]
    public void Scripts_without_upgraded_script_effects_keep_the_record_title_unupgraded()
    {
        AssertScriptRecordHasNoUpgradedVersion<BombardChibiScript, BombardChibiScriptRecord>();
        AssertScriptRecordHasNoUpgradedVersion<BurningLinkedCampsScript, BurningLinkedCampsScriptRecord>();
        AssertScriptRecordHasNoUpgradedVersion<EunuchScript, EunuchScriptRecord>();
        AssertScriptRecordHasNoUpgradedVersion<NeutralAmbushScript, NeutralAmbushScriptRecord>();
        AssertScriptRecordHasNoUpgradedVersion<OneManOneCity, OneManOneCityScriptRecord>();
        AssertScriptRecordHasNoUpgradedVersion<PartTimeGeneralScript, PartTimeGeneralScriptRecord>();
    }

    [Fact]
    public void Script_effect_card_previews_are_nested_after_their_script_record()
    {
        AssertRecordOwnsDerivedCard<ChickenFootCheeseScript, ChickenFootCheeseScriptRecord,
            ChickenFootCheeseScriptStrikePreview>();
        AssertRecordOwnsDerivedCard<ServantScript, ServantScriptRecord, ThrowHimOut>();
        AssertRecordOwnsDerivedCard<NeutralAmbushScript, NeutralAmbushScriptRecord, GiantRock>();
        AssertRecordOwnsDerivedCard<NeutralAmbushScript, NeutralAmbushScriptRecord, SalvoStrike>();
        AssertRecordOwnsDerivedCard<NightRaidWuchaoScript, NightRaidWuchaoScriptRecord,
            NightRaidWuchaoScriptStarryNightStrikePreview>();
    }

    private void AssertUpgradePreviewIsGreen<TSource, TTarget>(bool expectDescriptionHighlight = true)
        where TSource : CardModel
        where TTarget : CardModel
    {
        TSource source = Combat.CreateCard<TSource>(Player);
        source.UpgradePreviewType = CardUpgradePreviewType.Deck;
        source.UpgradeInternal();
        CardModel target = GetHoverCard<TTarget>(source);

        Assert.True(target.IsUpgraded);
        Assert.NotEqual(CardUpgradePreviewType.None, target.UpgradePreviewType);
        string description = target.GetDescriptionForUpgradePreview();
        if (expectDescriptionHighlight)
        {
            Assert.Contains("[green]", description);
        }
        else
        {
            Assert.DoesNotContain("[green]", description);
        }
    }

    private void AssertScriptRecordUpgradePreviewIsGreen<TSource, TRecord>(params string[] changedTexts)
        where TSource : ScriptCardTemplate
        where TRecord : ScriptRecordCardTemplate
    {
        TSource source = Combat.CreateCard<TSource>(Player);
        source.UpgradePreviewType = CardUpgradePreviewType.Deck;
        source.UpgradeInternal();
        TRecord record = (TRecord)GetHoverCard<TRecord>(source);

        Assert.True(record.IsUpgraded);
        Assert.True(record.SourceUpgraded);
        Assert.True(record.HasUpgradedVersion);
        Assert.NotEqual(CardUpgradePreviewType.None, record.UpgradePreviewType);
        Assert.EndsWith("+", record.Title);
        Assert.Contains(
            $"[green]{record.Title}[/green]",
            source.GetDescriptionForUpgradePreview());
        string description = record.GetDescriptionForUpgradePreview();
        int searchStart = 0;
        foreach (string changedText in changedTexts)
        {
            string highlightedText = $"[green]{changedText}[/green]";
            int match = description.IndexOf(highlightedText, searchStart, StringComparison.Ordinal);
            Assert.True(match >= 0, $"Expected '{highlightedText}' in '{description}'.");
            searchStart = match + highlightedText.Length;
        }
    }

    private void AssertScriptRecordHasNoUpgradedVersion<TSource, TRecord>()
        where TSource : ScriptCardTemplate
        where TRecord : ScriptRecordCardTemplate
    {
        TSource source = Combat.CreateCard<TSource>(Player);
        source.UpgradePreviewType = CardUpgradePreviewType.Deck;
        source.UpgradeInternal();
        TRecord record = (TRecord)GetHoverCard<TRecord>(source);

        Assert.True(source.IsUpgraded);
        Assert.True(record.SourceUpgraded);
        Assert.False(record.HasUpgradedVersion);
        Assert.False(record.IsUpgraded);
		Assert.False(record.Title.EndsWith("+", StringComparison.Ordinal));
        Assert.NotEqual(CardUpgradePreviewType.None, record.UpgradePreviewType);
        string sourceDescription = source.GetDescriptionForUpgradePreview();
        Assert.Contains($"[gold]{record.Title}[/gold]", sourceDescription);
        Assert.DoesNotContain($"[green]{record.Title}", sourceDescription);
    }

    private void AssertRecordOwnsDerivedCard<TSource, TRecord, TDerived>()
        where TSource : ScriptCardTemplate
        where TRecord : ScriptRecordCardTemplate
        where TDerived : CardModel
    {
        TSource source = Combat.CreateCard<TSource>(Player);
        CardHoverTip[] sourceCards = source.HoverTips.OfType<CardHoverTip>().ToArray();
        int recordIndex = Array.FindIndex(sourceCards, tip => tip.Card is TRecord);
        int derivedIndex = Array.FindIndex(sourceCards, tip => tip.Card is TDerived);

        Assert.True(recordIndex >= 0);
        Assert.True(derivedIndex > recordIndex);
        TRecord record = Assert.IsType<TRecord>(sourceCards[recordIndex].Card);
        Assert.Contains(record.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is TDerived);
    }

    private static CardModel GetHoverCard<TTarget>(CardModel source)
        where TTarget : CardModel =>
        Assert.Single(
            source.HoverTips
                .OfType<CardHoverTip>()
                .Where(tip => tip.Card is TTarget))
            .Card;
}
