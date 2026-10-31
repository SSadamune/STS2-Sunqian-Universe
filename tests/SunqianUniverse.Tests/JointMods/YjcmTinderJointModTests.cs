using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public abstract class YjcmTinderJointModTestSuite : SunqianCombatTestSuite
{
    private static readonly ModelId HuoGongCardId =
        new("CARD", "YJCM_CARD_HUO_GONG");

    private protected async Task AssertTinderHuoGongScenario(string expectedMode)
    {
        Assert.True(YjcmPublicApiInterop.IsReady);
		Assert.Equal(
			expectedMode == "InlineKeywords",
			YjcmPublicApiInterop.IsYjcmCharacter(Player.Character));
        const int tinderAmount = 4;
        await ApplyPower<TinderPower>(Player.Creature, tinderAmount);
        var tinder = Assert.IsType<TinderPower>(
            Player.Creature.GetPower<TinderPower>());
        IHoverTip huoGongTip = YjcmPublicApiInterop.CreateHuoGongHoverTip();
        string huoGongTitle = Assert.IsType<HoverTip>(huoGongTip).Title
            ?? throw new InvalidOperationException("Huo Gong hover title is missing.");
        string description = FormatPowerDescription(tinder);
        IHoverTip[] hoverTips = tinder.HoverTips.ToArray();
		string linkDescription = SquCommonL10n.YjcmTinderLibraryHover().GetFormattedText();

		if (expectedMode == "InlineKeywords")
        {
            Assert.Contains(huoGongTitle, description);
			Assert.Contains("[purple]", description);
            Assert.Contains(hoverTips, tip => tip.Id == huoGongTip.Id);
			Assert.DoesNotContain(
				hoverTips.OfType<HoverTip>(),
				tip => tip.Description == linkDescription);
        }
        else
        {
            Assert.DoesNotContain(huoGongTitle, description);
			Assert.DoesNotContain("[purple]", description);
            Assert.DoesNotContain(hoverTips, tip => tip.Id == huoGongTip.Id);
			if (expectedMode == "HoverOnly")
			{
				Assert.Contains(
					hoverTips.OfType<HoverTip>(),
					tip => tip.Description == linkDescription);
			}
			else
			{
				Assert.DoesNotContain(
					hoverTips.OfType<HoverTip>(),
					tip => tip.Description == linkDescription);
			}
        }

        foreach (CardModel existing in PileType.Hand.GetPile(Player).Cards.ToArray())
        {
            await CardPileCmd.Add(existing, PileType.Discard);
        }

        var discarded = await AddToHand<Dazed>();
        CardModel huoGong = await AddToHand(HuoGongCardId);
        int baseHuoGong = huoGong.DynamicVars["Stacks"].IntValue;
        await Play(huoGong, EnemyAt(0));
        AssertInPile(discarded, PileType.Discard);
        PowerModel appliedHuoGong = Assert.Single(
            EnemyAt(0).Powers.Where(YjcmPublicApiInterop.IsHuoGongPower));
        // Huo Gong immediately discards Dazed after applying the power. That
        // discard triggers Fire Attack once and consumes one of its stacks.
        Assert.Equal(baseHuoGong + tinderAmount - 1, appliedHuoGong.Amount);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}

public sealed class YjcmTinderPartyJointModTests : YjcmTinderJointModTestSuite
{
    private static readonly ModelId WuZhugeliangId =
        new("CHARACTER", "YJCM_CHARACTER_WU_ZHUGELIANG");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer(ModelDb.GetById<CharacterModel>(WuZhugeliangId), 2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-party-ui");

    [Fact]
	public Task Non_Yjcm_local_player_gets_hover_only_mode_when_party_contains_Yjcm_character() =>
		AssertTinderHuoGongScenario("HoverOnly");

	[Fact]
	public void All_cards_that_reference_Tinder_propagate_its_mod_link_hover()
	{
		string linkDescription = SquCommonL10n.YjcmTinderLibraryHover().GetFormattedText();
		CardModel[] cards =
		[
			Combat.CreateCard<BelovedPartner>(Player),
			Combat.CreateCard<BowangParadoxScript>(Player),
			Combat.CreateCard<BurnAfterReading>(Player),
			Combat.CreateCard<BurningLinkedCampsScript>(Player),
			Combat.CreateCard<DigRaid>(Player),
			Combat.CreateCard<RelocateCapital>(Player),
		];

		foreach (CardModel card in cards)
		{
			Assert.Contains(
				card.HoverTips.OfType<HoverTip>(),
				tip => tip.Description == linkDescription);
		}
	}

	[Fact]
	public async Task Shangfanggu_sigh_uses_hover_only_mode_for_non_Yjcm_local_player()
	{
		var card = await AddToHand<ShangfangguSigh>();
		IHoverTip huoGongTip = YjcmPublicApiInterop.CreateHuoGongHoverTip();
		string huoGongTitle = Assert.IsType<HoverTip>(huoGongTip).Title!;
		string description = card.GetDescriptionForPile(PileType.Hand);
		string linkDescription = SquCommonL10n.YjcmGoodFireLibraryHover().GetFormattedText();

		Assert.DoesNotContain(huoGongTitle, description);
		Assert.DoesNotContain("[purple]", description);
		Assert.DoesNotContain(card.HoverTips, tip => tip.Id == huoGongTip.Id);
		Assert.Contains(
			card.HoverTips.OfType<HoverTip>(),
			tip => tip.Description == linkDescription);
	}
}

public sealed class YjcmTinderNoPartyJointModTests : YjcmTinderJointModTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer<Silent>(2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-no-party-ui");

    [Fact]
    public Task Multiplayer_without_Yjcm_character_gets_effect_but_no_ui() =>
		AssertTinderHuoGongScenario("None");
}

public sealed class YjcmTinderSoloJointModTests : YjcmTinderJointModTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-solo-no-ui");

    [Fact]
    public Task Solo_non_Yjcm_character_gets_effect_but_no_ui() =>
		AssertTinderHuoGongScenario("None");
}

public sealed class YjcmTinderLocalJointModTests : YjcmTinderJointModTestSuite
{
	private static readonly ModelId WuZhugeliangId =
		new("CHARACTER", "YJCM_CHARACTER_WU_ZHUGELIANG");

	protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
		battle
			.Player(ModelDb.GetById<CharacterModel>(WuZhugeliangId))
			.AddEnemy<BigDummy>()
			.WithSeed("yjcm-tinder-local-inline-keywords");

	[Fact]
	public Task Yjcm_local_player_gets_inline_keyword_mode() =>
		AssertTinderHuoGongScenario("InlineKeywords");
}
