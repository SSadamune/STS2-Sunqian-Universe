using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class EunuchScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-eunuch-script");
    }

    [Fact]
    public async Task EU_01_play_draws_cards_and_grants_temporary_ethereal_until_each_card_is_played()
    {
        var movedCard = await AddToDrawPileTop<FlameStrike>();
        var playedCard = await AddToDrawPileTop<DefendLongtao>();

        await PlayAndActivateScript(await AddToHand<EunuchScript>());

        AssertTemporaryEtherealInHand(playedCard);
        AssertTemporaryEtherealInHand(movedCard);
		string playedDescription = playedCard.GetDescriptionForPile(PileType.Hand);
		Assert.Contains(KeywordMarker(CardKeyword.Ethereal), playedDescription);
		Assert.DoesNotContain(KeywordMarker(CardKeyword.Retain), playedDescription);
		Assert.DoesNotContain("小黄门的口信", playedDescription);
		HoverTip markerTip = Assert.Single(
			playedCard.HoverTips.OfType<HoverTip>(),
			tip => tip.Id == HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage).Id);
		Assert.Equal("小黄门的口信", markerTip.Title);
		Assert.Equal(
			"因[gold]小黄门[/gold]的效果，这张牌在被打出前具有[gold]保留[/gold]。",
			markerTip.Description);

        await CardPileCmd.Add(movedCard, PileType.Discard);
        AssertInPile(movedCard, PileType.Discard);
        AssertTemporaryEthereal(movedCard);

        await Play(playedCard);
        AssertInPile(playedCard, PileType.Discard);
        Assert.False(playedCard.HasEunuchMessage());
        Assert.DoesNotContain(CardKeyword.Ethereal, playedCard.Keywords);
		Assert.DoesNotContain(
			playedCard.HoverTips,
			tip => tip.Id == HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage).Id);
    }

    [Fact]
    public async Task EU_02_active_script_retains_all_ethereal_cards_and_suppresses_ethereal_exhaust_effects()
    {
        var drawnSecond = await AddToDrawPileTop<DefendLongtao>();
        var drawnFirst = await AddToDrawPileTop<FlameStrike>();
        var intrinsicEthereal = await AddToHand<WhyDoubtWei>();

        await PlayAndActivateScript(await AddToHand<EunuchScript>());
        await EndTurn();

        AssertInPile(drawnFirst, PileType.Hand);
        AssertInPile(drawnSecond, PileType.Hand);
        AssertInPile(intrinsicEthereal, PileType.Hand);
        Assert.True(intrinsicEthereal.ShouldRetainThisTurn);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Null(Player.Creature.GetPower<WeakPower>());
    }

    [Fact]
    public async Task EU_03_virtual_retain_participates_in_after_flush_retain_hooks()
    {
        var retained = await AddToHand<KeepCalm>();
        CardCmd.RemoveKeyword(retained, CardKeyword.Retain);
        CardCmd.ApplyKeyword(retained, CardKeyword.Ethereal);
        int energyBefore = retained.DynamicVars.Energy.IntValue;
        await AddToDrawPileTop<DefendLongtao>();
        await AddToDrawPileTop<FlameStrike>();

        await PlayAndActivateScript(await AddToHand<EunuchScript>());
        Assert.True(retained.ShouldRetainThisTurn);
        await EndTurn();

        AssertInPile(retained, PileType.Hand);
        Assert.Equal(
            energyBefore + KeepCalm.CanonicalRetainBonus,
            retained.DynamicVars.Energy.IntValue);
    }

    [Fact]
    public async Task EU_04_bowang_paradox_does_not_discard_an_ethereal_attack_while_eunuch_is_active()
    {
        await MegaCrit.Sts2.Core.Commands.PlayerCmd.SetEnergy(10, Player);
        await PlayAndActivateScript(await AddToHand<ServantScript>());
        var protectedAttack = await AddToDrawPileTop<FlameStrike>();
        await AddToDrawPileTop<DefendLongtao>();
        await PlayAndActivateScript(await AddToHand<EunuchScript>());
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxAdvanceScriptRecord);

        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(await AddToHand<BowangParadoxScript>());
        }
        AssertAllSelectionsConsumed(selector);

        AssertInPile(protectedAttack, PileType.Hand);
        Assert.True(protectedAttack.ShouldRetainThisTurn);
    }

    [Fact]
    public async Task EU_05_deactivation_does_not_immediately_move_cards_but_restores_normal_ethereal_cleanup()
    {
        var second = await AddToDrawPileTop<DefendLongtao>();
        var first = await AddToDrawPileTop<FlameStrike>();
        await PlayAndActivateScript(await AddToHand<EunuchScript>());

        await PlayAndActivateScript(await AddToHand<ServantScript>());

        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        Assert.False(first.ShouldRetainThisTurn);
        Assert.False(second.ShouldRetainThisTurn);
		HoverTip inactiveMessage = Assert.Single(
			first.HoverTips.OfType<HoverTip>(),
			tip => tip.Id == HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage).Id);
		Assert.Contains("[gold]虚无[/gold]", inactiveMessage.Description);

        await EndTurn();

        AssertInPile(first, PileType.Exhaust);
        AssertInPile(second, PileType.Exhaust);
    }

    [Fact]
    public async Task EU_06_ethereal_card_with_a_turn_end_effect_resolves_that_effect_and_returns_to_hand()
    {
        await AddToDrawPileTop<DefendLongtao>();
        var burn = await AddToDrawPileTop<Burn>();
        await PlayAndActivateScript(await AddToHand<EunuchScript>());
        int hpBefore = Player.Creature.CurrentHp;

        await EndTurn();

        Assert.Equal(hpBefore - burn.DynamicVars.Damage.IntValue, Player.Creature.CurrentHp);
        AssertTemporaryEtherealInHand(burn);
    }

    [Fact]
    public async Task EU_07_intrinsic_ethereal_is_neither_marked_nor_removed_and_does_not_gain_a_retain_line()
    {
        var intrinsicEthereal = await AddToHand<WhyDoubtWei>();
        string descriptionBeforeActivation =
            intrinsicEthereal.GetDescriptionForPile(PileType.Hand);
        await AddToDrawPileTop<DefendLongtao>();
        await AddToDrawPileTop<FlameStrike>();

        await PlayAndActivateScript(await AddToHand<EunuchScript>());

        Assert.False(intrinsicEthereal.HasEunuchMessage());
        Assert.Contains(CardKeyword.Ethereal, intrinsicEthereal.Keywords);
        Assert.True(intrinsicEthereal.ShouldRetainThisTurn);
        string activeDescription = intrinsicEthereal.GetDescriptionForPile(PileType.Hand);
        Assert.Equal(
            CountOccurrences(descriptionBeforeActivation, KeywordMarker(CardKeyword.Ethereal)),
            CountOccurrences(activeDescription, KeywordMarker(CardKeyword.Ethereal)));
        Assert.Equal(
            CountOccurrences(descriptionBeforeActivation, KeywordMarker(CardKeyword.Retain)),
            CountOccurrences(activeDescription, KeywordMarker(CardKeyword.Retain)));
        Assert.DoesNotContain("小黄门的口信", activeDescription);

        await Play(intrinsicEthereal, EnemyAt(0));

        Assert.Contains(CardKeyword.Ethereal, intrinsicEthereal.Keywords);
        Assert.False(intrinsicEthereal.HasEunuchMessage());
    }

    [Fact]
    public async Task EU_08_intrinsic_retain_survives_message_cleanup_without_duplicate_card_text()
    {
        await AddToDrawPileTop<DefendLongtao>();
        var intrinsicRetain = await AddToDrawPileTop<KeepCalm>();
        string descriptionBeforeMessage =
            intrinsicRetain.GetDescriptionForPile(PileType.Hand);

        await PlayAndActivateScript(await AddToHand<EunuchScript>());

        AssertTemporaryEtherealInHand(intrinsicRetain);
        Assert.Contains(CardKeyword.Retain, intrinsicRetain.Keywords);
        string activeDescription = intrinsicRetain.GetDescriptionForPile(PileType.Hand);
        Assert.Equal(
            CountOccurrences(descriptionBeforeMessage, KeywordMarker(CardKeyword.Retain)),
            CountOccurrences(activeDescription, KeywordMarker(CardKeyword.Retain)));
        Assert.Equal(
            CountOccurrences(descriptionBeforeMessage, KeywordMarker(CardKeyword.Ethereal)) + 1,
            CountOccurrences(activeDescription, KeywordMarker(CardKeyword.Ethereal)));
        Assert.DoesNotContain("小黄门的口信", activeDescription);

        await Play(intrinsicRetain);

        Assert.Contains(CardKeyword.Retain, intrinsicRetain.Keywords);
        Assert.DoesNotContain(CardKeyword.Ethereal, intrinsicRetain.Keywords);
        Assert.False(intrinsicRetain.HasEunuchMessage());
        Assert.DoesNotContain(
            intrinsicRetain.HoverTips,
            tip => tip.Id == HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage).Id);
    }

    private static void AssertTemporaryEtherealInHand(CardModel card)
    {
        AssertInPile(card, PileType.Hand);
        AssertTemporaryEthereal(card);
        Assert.True(card.ShouldRetainThisTurn);
    }

    private static void AssertTemporaryEthereal(CardModel card)
    {
        Assert.True(card.HasEunuchMessage());
        Assert.Contains(CardKeyword.Ethereal, card.Keywords);
    }

    private static string KeywordMarker(CardKeyword keyword)
    {
        HoverTip tip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(keyword));
        return $"[gold]{tip.Title}[/gold]";
    }

    private static int CountOccurrences(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
