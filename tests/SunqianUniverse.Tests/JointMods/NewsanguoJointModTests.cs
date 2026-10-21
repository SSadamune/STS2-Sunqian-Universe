using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Characters;
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
		AssertInlineKeywordMode(wine);
		AssertInlineKeywordMode(grandGoblet);

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

    [Fact]
    public async Task Normal_thinking_uses_real_heavens_force_api_when_ethereal()
    {
        Assert.True(NewsanguoPublicApiInterop.IsReady);
        string heavensForceTitle = Assert.IsType<HoverTip>(
            NewsanguoPublicApiInterop.CreateHeavensForceHoverTip()).Title
            ?? throw new InvalidOperationException("Heaven's Force hover title is missing.");
        var card = await AddToHand<NormalThinking>();
        Assert.Contains(heavensForceTitle, card.GetDescriptionForPile(PileType.Hand));

        await NewsanguoPublicApiInterop.AddHeavensForce(
            new BlockingPlayerChoiceContext(),
            Player,
            NormalThinking.HeavensForceLoss,
            card);
        int before = NewsanguoPublicApiInterop.GetHeavensForce(Player);

        await CardCmd.Exhaust(
            new BlockingPlayerChoiceContext(),
            card,
            causedByEthereal: true);

        Assert.Equal(
            before - NormalThinking.HeavensForceLoss,
            NewsanguoPublicApiInterop.GetHeavensForce(Player));
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

	private static void AssertInlineKeywordMode(CardModel card)
	{
		IHoverTip drunkenMightTip = NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip();
		string linkDescription = SquCommonL10n.NewsanguoVigorSwapLibraryHover()
			.GetFormattedText();
		Assert.Contains("[purple]", card.GetDescriptionForPile(PileType.Hand));
		Assert.Contains(card.HoverTips, tip => tip.Id == drunkenMightTip.Id);
		Assert.DoesNotContain(
			card.HoverTips.OfType<HoverTip>(),
			tip => tip.Description == linkDescription);
	}

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}

public sealed class NewsanguoPartyJointModTests : SunqianCombatTestSuite
{
	private static readonly ModelId CaoWeiCharacterId =
		new("CHARACTER", "NEWSANGUO_CHARACTER_CAO_WEI_CHARACTER");

	protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
		battle
			.AddPlayer<Ironclad>(1)
			.AddPlayer(ModelDb.GetById<CharacterModel>(CaoWeiCharacterId), 2)
			.LocalNetId(1)
			.AddEnemy<BigDummy>()
			.WithSeed("joint-newsanguo-party-hover-only");

	[Fact]
	public async Task Non_Newsanguo_local_player_gets_hover_only_mode()
	{
		Assert.False(NewsanguoPublicApiInterop.IsNewsanguoCharacter(Player.Character));
		Assert.True(NewsanguoPublicApiInterop.IsNewsanguoCharacter(PlayerWithNetId(2).Character));
		string vigorTitle = ModelDb.Power<VigorPower>().Title.GetFormattedText();
		string drunkenMightTitle = Assert.IsType<HoverTip>(
			NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip()).Title!;
		string linkDescription = SquCommonL10n.NewsanguoVigorSwapLibraryHover()
			.GetFormattedText();
		CardModel[] cards =
		[
			await AddToHand<Wine>(),
			await AddToHand<GrandGoblet>(),
		];

		foreach (CardModel card in cards)
		{
			string description = card.GetDescriptionForPile(PileType.Hand);
			Assert.Contains(vigorTitle, description);
			Assert.DoesNotContain(drunkenMightTitle, description);
			Assert.DoesNotContain("[purple]", description);
			Assert.Contains(
				card.HoverTips.OfType<HoverTip>(),
				tip => tip.Description == linkDescription);
			Assert.DoesNotContain(
				card.HoverTips,
				tip => tip.Id == NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip().Id);
		}
	}
}
