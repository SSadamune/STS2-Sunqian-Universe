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
    public void Unrelated_card_and_power_do_not_gain_a_collection_tip()
    {
        string expectedId = HoverTipFactory.FromKeyword(SquKeywords.ScriptCollectionId.GetModCardKeyword()).Id;
        Assert.DoesNotContain(ModelDb.Card<DefendLongtao>().HoverTips, tip => tip.Id == expectedId);
        Assert.DoesNotContain(ModelDb.Power<GuanDiFormPower>().HoverTips, tip => tip.Id == expectedId);
    }
}
