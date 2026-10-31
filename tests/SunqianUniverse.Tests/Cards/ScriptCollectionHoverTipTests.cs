using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using STS2RitsuLib.Keywords;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ScriptCollectionHoverTipTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-collection-hover-tips");

    [Theory]
    [InlineData(typeof(GuanDiForm))]
    [InlineData(typeof(BowangParadoxScript))]
    [InlineData(typeof(ExecutionCommanderScriptRecord))]
    public void Cards_mentioning_the_collection_directly_or_in_keyword_text_include_one_collection_tip(Type type)
    {
        var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
        string expectedId = HoverTipFactory.FromKeyword(SquKeywords.ScriptCollectionId.GetModCardKeyword()).Id;
        Assert.Single(card.HoverTips, tip => tip.Id == expectedId);
    }

    [Theory]
    [InlineData(typeof(GuanYuCivilVer), typeof(GuanDiForm))]
    [InlineData(typeof(GuanYuMartialVer), typeof(GuanDiForm))]
    [InlineData(typeof(HeadsmanScriptRecord), typeof(HeadsmanScript))]
    [InlineData(typeof(BowangParadoxAdvanceScriptRecord), typeof(BowangParadoxScript))]
    public void Material_reuse_hover_names_the_unupgraded_source_card(Type recordType, Type sourceType)
    {
        var record = (ScriptRecordCardTemplate)ModelDb.GetById<CardModel>(ModelDb.GetId(recordType)).ToMutable();
        Upgrade(record);
        string sourceName = ModelDb.GetById<CardModel>(ModelDb.GetId(sourceType)).Title;
        var tip = Assert.Single(record.HoverTips.OfType<HoverTip>(), tip => tip.Title == "素材复用");
        Assert.Equal($"如果[gold]剧本集[/gold]中已有此剧本，当你再次打出[gold]{sourceName}[/gold]时，会将此剧本[gold]升级[/gold]。", tip.Description);
    }

    [Theory]
    [InlineData(typeof(GuanDiForm))]
    [InlineData(typeof(BowangParadoxScript))]
    public void Script_addition_text_marks_upgraded_scripts_green(Type sourceType)
    {
        var source = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(sourceType)).ToMutable();
        source.UpgradePreviewType = MegaCrit.Sts2.Core.Entities.Cards.CardUpgradePreviewType.Deck;
        source.UpgradeInternal();
        Assert.Contains("[green]剧本+[/green]", source.GetDescriptionForUpgradePreview());
    }

    [Theory]
    [InlineData(typeof(GuanYuCivilVer), "武关羽")]
    [InlineData(typeof(GuanYuMartialVer), "文关羽")]
    public void Guan_yu_exclusion_note_is_direct_only_and_keeps_other_nested_tips(Type recordType, string partner)
    {
        var record = (ScriptRecordCardTemplate)ModelDb.GetById<CardModel>(ModelDb.GetId(recordType));
        var note = Assert.Single(record.HoverTips.OfType<HoverTip>(), tip => tip.Title == "Tips");
        Assert.Equal($"[red]不能[/red]与[gold]{partner}[/gold]同时激活。", note.Description);
        Assert.DoesNotContain("同时激活", record.GetDescriptionForPile(MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck));
        Assert.DoesNotContain(record.NestedHoverTips, tip => tip.Id == note.Id);
        Assert.DoesNotContain(ModelDb.Card<GuanDiForm>().HoverTips, tip => tip.Id == note.Id);
        Assert.Contains(ModelDb.Card<GuanDiForm>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "素材复用");
        Assert.Contains("将两种互斥的", ModelDb.Card<GuanDiForm>().GetDescriptionForPile(
            MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck));
    }

    [Fact]
    public void Parent_scripts_omit_keyword_tips_that_belong_only_to_their_records()
    {
        AssertTipsHidden<BowangParadoxScript>("活力", "火种", "灼烧", "保留");
        Assert.Contains(ModelDb.Card<BowangParadoxAdvanceScriptRecord>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "活力");
        Assert.Contains(ModelDb.Card<BowangParadoxAdvanceScriptRecord>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "保留");
        Assert.Contains(ModelDb.Card<BowangParadoxArsonScriptRecord>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "火种");
        Assert.Contains(ModelDb.Card<BowangParadoxArsonScriptRecord>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "灼烧");
        Assert.Contains(ModelDb.Card<BowangParadoxArsonScriptRecord>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "保留");

        AssertTipsHidden<GuanDiForm>("敏捷", "格挡", "活力", "能量", "力量");
        Assert.Contains(ModelDb.Card<GuanDiForm>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "剧本");
        Assert.Contains(ModelDb.Card<GuanYuCivilVer>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "敏捷");
        Assert.Contains(ModelDb.Card<GuanYuCivilVer>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "格挡");
        Assert.DoesNotContain(ModelDb.Card<GuanYuCivilVer>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "活力");
        Assert.Contains(ModelDb.Card<GuanYuMartialVer>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "力量");
        Assert.Contains(ModelDb.Card<GuanYuMartialVer>().HoverTips.OfType<HoverTip>(), tip => tip.Title == "格挡");
    }

    private static void AssertTipsHidden<TCard>(params string[] titles) where TCard : CardModel
    {
        var shown = ModelDb.Card<TCard>().HoverTips.OfType<HoverTip>().Select(tip => tip.Title).ToHashSet();
        Assert.DoesNotContain(shown, title => titles.Contains(title));
    }

    [Fact]
    public void Unrelated_card_and_power_do_not_gain_a_collection_tip()
    {
        string expectedId = HoverTipFactory.FromKeyword(SquKeywords.ScriptCollectionId.GetModCardKeyword()).Id;
        Assert.DoesNotContain(ModelDb.Card<DefendLongtao>().HoverTips, tip => tip.Id == expectedId);
        Assert.DoesNotContain(ModelDb.Power<GuanDiFormPower>().HoverTips, tip => tip.Id == expectedId);
    }
}
