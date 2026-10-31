using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class InactiveJointModTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer<Silent>(2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("joint-mods-inactive-for-vanilla-party");

    [Fact]
    public async Task Yjcm_loaded_without_Yjcm_characters_keeps_Shangfanggu_sigh_unchanged()
    {
        Assert.True(YjcmPublicApiInterop.IsReady);
        Player ally = PlayerWithNetId(2);
        Assert.False(YjcmPublicApiInterop.IsYjcmCharacter(Player.Character));
        Assert.False(YjcmPublicApiInterop.IsYjcmCharacter(ally.Character));

        PowerModel huoGong = Assert.Single(
            ModelDb.AllPowers.Where(YjcmPublicApiInterop.IsHuoGongPower));
        IHoverTip huoGongTip = YjcmPublicApiInterop.CreateHuoGongHoverTip();
        string huoGongTitle = Assert.IsType<HoverTip>(huoGongTip).Title
            ?? throw new InvalidOperationException("Huo Gong hover title is missing.");
        var playerCard = Upgrade(await AddToHand<ShangfangguSigh>(Player));
        var allyCard = Upgrade(await AddToHand<ShangfangguSigh>(ally));
        AssertNoInteropText(
            playerCard,
            huoGongTip,
            SquCommonL10n.YjcmGoodFireHoverTitle().GetFormattedText(),
            huoGongTitle);
        AssertNoInteropText(
            allyCard,
            huoGongTip,
            SquCommonL10n.YjcmGoodFireHoverTitle().GetFormattedText(),
            huoGongTitle);

        await Play(playerCard);
        await Play(allyCard);

        AssertGoodFireKeepsOnlyOriginalEffect(Player, playerCard, huoGong);
        AssertGoodFireKeepsOnlyOriginalEffect(ally, allyCard, huoGong);
        Assert.DoesNotContain(
            huoGongTitle,
            FormatPowerDescription(Player.Creature.GetPower<GoodFirePower>()!));
        Assert.DoesNotContain(
            huoGongTitle,
            FormatPowerDescription(ally.Creature.GetPower<GoodFirePower>()!));
    }

    [Fact]
    public async Task Newsanguo_loaded_without_Newsanguo_characters_keeps_cards_unchanged()
    {
        Assert.True(NewsanguoPublicApiInterop.IsReady);
        Player ally = PlayerWithNetId(2);
        Assert.False(NewsanguoPublicApiInterop.IsNewsanguoCharacter(Player.Character));
        Assert.False(NewsanguoPublicApiInterop.IsNewsanguoCharacter(ally.Character));

        IHoverTip drunkenMightTip = NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip();
        string drunkenMightTitle = Assert.IsType<HoverTip>(drunkenMightTip).Title
            ?? throw new InvalidOperationException("Drunken Might hover title is missing.");
        string vigorTitle = ModelDb.Power<VigorPower>().Title.GetFormattedText();
        int playerDazedBefore = DazedCount(Player);
        int allyDazedBefore = DazedCount(ally);
        var playerCard = await AddToHand<Wine>(Player);
        var allyCard = await AddToHand<Wine>(ally);
        var playerGoblet = await AddToHand<GrandGoblet>(Player);
        var allyGoblet = await AddToHand<GrandGoblet>(ally);
        AssertNoInteropText(
            playerCard,
            drunkenMightTip,
            SquCommonL10n.NewsanguoVigorSwapHoverTitle().GetFormattedText(),
            drunkenMightTitle);
        AssertNoInteropText(
            allyCard,
            drunkenMightTip,
            SquCommonL10n.NewsanguoVigorSwapHoverTitle().GetFormattedText(),
            drunkenMightTitle);
        AssertNoInteropText(
            playerGoblet,
            drunkenMightTip,
            SquCommonL10n.NewsanguoVigorSwapHoverTitle().GetFormattedText(),
            drunkenMightTitle);
        AssertNoInteropText(
            allyGoblet,
            drunkenMightTip,
            SquCommonL10n.NewsanguoVigorSwapHoverTitle().GetFormattedText(),
            drunkenMightTitle);
        Assert.Contains(vigorTitle, playerCard.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(vigorTitle, allyCard.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(vigorTitle, playerGoblet.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(vigorTitle, allyGoblet.GetDescriptionForPile(PileType.Hand));

        await Play(playerGoblet);
        await Play(allyGoblet);
        int playerEnergyBefore = Player.PlayerCombatState!.Energy;
        int allyEnergyBefore = ally.PlayerCombatState!.Energy;
        await Play(playerCard);
        await Play(allyCard);

        AssertGrandGobletKeepsOriginalText(Player, drunkenMightTitle, vigorTitle);
        AssertGrandGobletKeepsOriginalText(ally, drunkenMightTitle, vigorTitle);
        AssertWineKeepsOriginalEffect(Player, playerEnergyBefore, playerDazedBefore);
        AssertWineKeepsOriginalEffect(ally, allyEnergyBefore, allyDazedBefore);
    }

    private void AssertGoodFireKeepsOnlyOriginalEffect(
        Player owner,
        ShangfangguSigh card,
        PowerModel huoGong)
    {
        var goodFire = Assert.IsType<GoodFirePower>(
            owner.Creature.GetPower<GoodFirePower>());
        decimal expectedBurningMultiplier =
            1m + ShangfangguSigh.UpgradedBonusPercent / 100m;
        Assert.Equal(
            expectedBurningMultiplier,
            goodFire.ModifyPowerAmountGivenMultiplicative(
                ModelDb.Power<BurningPower>(),
                owner.Creature,
                3m,
                EnemyAt(0),
                card));
        Assert.Equal(
            1m,
            goodFire.ModifyPowerAmountGivenMultiplicative(
                huoGong,
                owner.Creature,
                3m,
                EnemyAt(0),
                card));
    }

    private static void AssertWineKeepsOriginalEffect(
        Player owner,
        int energyBefore,
        int dazedBefore)
    {
        Assert.Equal(energyBefore + Wine.BaseEnergy, owner.PlayerCombatState!.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(owner.Creature, Wine.BaseVigor);
        Assert.Equal(0, NewsanguoPublicApiInterop.GetDrunkenMight(owner));
        Assert.Equal(dazedBefore + 1, DazedCount(owner));
    }

    private static void AssertGrandGobletKeepsOriginalText(
        Player owner,
        string drunkenMightTitle,
        string vigorTitle)
    {
        var power = Assert.IsType<GrandGobletPower>(
            owner.Creature.GetPower<GrandGobletPower>());
        string description = FormatPowerDescription(power);
        Assert.Contains(vigorTitle, description);
        Assert.DoesNotContain(drunkenMightTitle, description);
    }

    private static void AssertNoInteropText(
        CardModel card,
        IHoverTip externalPowerTip,
        string libraryHoverTitle,
        string interopPowerTitle)
    {
        IHoverTip[] tips = card.HoverTips.ToArray();
        Assert.DoesNotContain(tips, tip => tip.Id == externalPowerTip.Id);
        Assert.DoesNotContain(
            tips,
            tip => tip is HoverTip textTip && textTip.Title == libraryHoverTitle);
        Assert.DoesNotContain(interopPowerTitle, card.GetDescriptionForPile(PileType.Hand));
    }

    private static int DazedCount(Player owner) =>
        PileType.Draw.GetPile(owner).Cards.OfType<Dazed>().Count();

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }

}
