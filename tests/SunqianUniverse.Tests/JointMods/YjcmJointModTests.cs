using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class YjcmJointModTests : SunqianCombatTestSuite
{
    private static readonly ModelId WuZhugeliangId =
        new("CHARACTER", "YJCM_CHARACTER_WU_ZHUGELIANG");
    private static readonly ModelId WeiDongZhuoId =
        new("CHARACTER", "YJCM_CHARACTER_WEI_DONG_ZHUO");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        CharacterModel wuZhugeliang = ModelDb.GetById<CharacterModel>(WuZhugeliangId);
        CharacterModel weiDongZhuo = ModelDb.GetById<CharacterModel>(WeiDongZhuoId);
        battle
            .AddPlayer(wuZhugeliang, 1)
            .AddPlayer(weiDongZhuo, 2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("joint-yjcm-shangfanggu-sigh");
    }

    [Fact]
    public async Task Shangfanggu_sigh_increases_real_Yjcm_fire_attack_for_both_characters()
    {
        Assert.True(
            YjcmPublicApiInterop.IsReady,
            "The installed Yjcm build must expose Yjcm.Api.YjcmPublicApi.");
        Assert.True(YjcmPublicApiInterop.IsYjcmCharacter(Player.Character));
        Assert.True(YjcmPublicApiInterop.IsYjcmCharacter(PlayerWithNetId(2).Character));
        PowerModel huoGong = Assert.Single(
            ModelDb.AllPowers.Where(YjcmPublicApiInterop.IsHuoGongPower));
        string huoGongTitle = Assert.IsType<HoverTip>(
            YjcmPublicApiInterop.CreateHuoGongHoverTip()).Title
            ?? throw new InvalidOperationException("Huo Gong hover title is missing.");
        var weiDongZhuo = PlayerWithNetId(2);
        var wuZhugeliangCard = Upgrade(await AddToHand<ShangfangguSigh>(Player));
        var weiDongZhuoCard = Upgrade(await AddToHand<ShangfangguSigh>(weiDongZhuo));
        Assert.Contains(
            huoGongTitle,
            wuZhugeliangCard.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(
            huoGongTitle,
            weiDongZhuoCard.GetDescriptionForPile(PileType.Hand));
		AssertInlineKeywordMode(wuZhugeliangCard, huoGongTitle);
		AssertInlineKeywordMode(weiDongZhuoCard, huoGongTitle);

        await Play(wuZhugeliangCard);
        await Play(weiDongZhuoCard);

        var wuZhugeliangGoodFire = Assert.IsType<GoodFirePower>(
            Player.Creature.GetPower<GoodFirePower>());
        var weiDongZhuoGoodFire = Assert.IsType<GoodFirePower>(
            weiDongZhuo.Creature.GetPower<GoodFirePower>());
        Assert.Contains(
            huoGongTitle,
            FormatPowerDescription(wuZhugeliangGoodFire));
        Assert.Contains(
            huoGongTitle,
            FormatPowerDescription(weiDongZhuoGoodFire));
        decimal expectedMultiplier =
            1m + ShangfangguSigh.UpgradedBonusPercent / 100m;
        Assert.Equal(
            expectedMultiplier,
            wuZhugeliangGoodFire.ModifyPowerAmountGivenMultiplicative(
                huoGong,
                Player.Creature,
                3m,
                EnemyAt(0),
                wuZhugeliangCard));
        Assert.Equal(
            expectedMultiplier,
            weiDongZhuoGoodFire.ModifyPowerAmountGivenMultiplicative(
                huoGong,
                weiDongZhuo.Creature,
                3m,
                EnemyAt(0),
                weiDongZhuoCard));
    }

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }

	private static void AssertInlineKeywordMode(CardModel card, string huoGongTitle)
	{
		IHoverTip huoGongTip = YjcmPublicApiInterop.CreateHuoGongHoverTip();
		string linkDescription = SquCommonL10n.YjcmGoodFireLibraryHover().GetFormattedText();
		string description = card.GetDescriptionForPile(PileType.Hand);
		Assert.Contains("[purple]", description);
		Assert.Contains(huoGongTitle, description);
		Assert.Contains(card.HoverTips, tip => tip.Id == huoGongTip.Id);
		Assert.DoesNotContain(
			card.HoverTips.OfType<HoverTip>(),
			tip => tip.Description == linkDescription);
	}
}
