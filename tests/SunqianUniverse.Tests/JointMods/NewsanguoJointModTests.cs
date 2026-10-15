using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class NewsanguoJointModTests : SunqianCombatTestSuite
{
    private static readonly ModelId CaoWeiCharacterId =
        new("CHARACTER", "NEWSANGUO_CHARACTER_CAO_WEI_CHARACTER");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        CharacterModel caoWei = ModelDb.GetById<CharacterModel>(CaoWeiCharacterId);
        battle
            .Player(caoWei)
            .AddEnemy<BigDummy>()
            .WithSeed("joint-newsanguo-wine");
    }

    [Fact]
    public async Task Wine_and_Grand_Goblet_use_real_Newsanguo_drunken_might()
    {
        Assert.True(NewsanguoPublicApiInterop.IsReady);
        Assert.True(NewsanguoPublicApiInterop.IsNewsanguoCharacter(Player.Character));
        string drunkenMightTitle = Assert.IsType<HoverTip>(
            NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip()).Title
            ?? throw new InvalidOperationException("Drunken Might hover title is missing.");
        string vigorTitle = ModelDb.Power<VigorPower>().Title.GetFormattedText();
        var wine = await AddToHand<Wine>();
        var grandGoblet = await AddToHand<GrandGoblet>();
        AssertUsesDrunkenMightText(wine, drunkenMightTitle, vigorTitle);
        AssertUsesDrunkenMightText(grandGoblet, drunkenMightTitle, vigorTitle);

        await Play(grandGoblet);
        var grandGobletPower = Assert.IsType<GrandGobletPower>(
            Player.Creature.GetPower<GrandGobletPower>());
        string powerDescription = FormatPowerDescription(grandGobletPower);
        Assert.Contains(drunkenMightTitle, powerDescription);
        Assert.DoesNotContain(vigorTitle, powerDescription);

        await Play(wine);

        Assert.Equal(Wine.BaseVigor, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }

    private static void AssertUsesDrunkenMightText(
        CardModel card,
        string drunkenMightTitle,
        string vigorTitle)
    {
        string description = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains(drunkenMightTitle, description);
        Assert.DoesNotContain(vigorTitle, description);
    }

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}
