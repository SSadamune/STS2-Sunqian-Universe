using MegaCrit.Sts2.Core.Models;
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
        var weiDongZhuo = PlayerWithNetId(2);
        var wuZhugeliangCard = Upgrade(await AddToHand<ShangfangguSigh>(Player));
        var weiDongZhuoCard = Upgrade(await AddToHand<ShangfangguSigh>(weiDongZhuo));

        await Play(wuZhugeliangCard);
        await Play(weiDongZhuoCard);

        var wuZhugeliangGoodFire = Assert.IsType<GoodFirePower>(
            Player.Creature.GetPower<GoodFirePower>());
        var weiDongZhuoGoodFire = Assert.IsType<GoodFirePower>(
            weiDongZhuo.Creature.GetPower<GoodFirePower>());
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
}
