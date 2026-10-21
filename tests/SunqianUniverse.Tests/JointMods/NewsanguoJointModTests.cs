using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Characters;
using Squ;
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

    [Fact]
    public async Task Combat_text_appends_drunken_might_without_the_library_hover()
    {
        string orText = SquCommonL10n.NewsanguoDrunkenMightOrText().GetFormattedText();
        string hover = SquCommonL10n.NewsanguoDrunkenMightEffectHover().GetFormattedText();
        var card = await AddToHand<WorkLifeBalance>();
        var keyword = HoverTipFactory.FromKeyword(SquKeywords.SupremeGeneral);

        Assert.Contains(orText, card.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(orText, Assert.IsType<HoverTip>(keyword).Description);
        Assert.DoesNotContain(
            card.HoverTips.OfType<HoverTip>(),
            tip => tip.Description == hover);
    }

    [Fact]
    public async Task Dig_raid_burning_and_cross_the_river_add_drunken_might_without_replacing_vigor()
    {
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await ApplyDrunkenMight(4);
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;

        await Play(Upgrade(await AddToHand<DigRaid>()), enemy);

        Assert.Equal(hp - DigRaid.UpgradedDamage - 4 - 4, enemy.CurrentHp);
        Assert.Equal(DigRaid.UpgradedBurning + 4 + 4, (int)(enemy.GetPower<BurningPower>()?.Amount ?? 0));
        Assert.Null(Player.Creature.GetPower<VigorPower>());

        await ApplyPower<CrossTheRiverPower>(Player.Creature, 1);
        await ApplyDrunkenMight(3);
        int drunken = NewsanguoPublicApiInterop.GetDrunkenMight(Player);
        int strikeHp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(strikeHp - 6 - drunken - drunken, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Spent_drunken_might_charges_block_and_is_restored_alongside_vigor()
    {
        var balance = await AddToHand<WorkLifeBalance>();
        await ApplyDrunkenMight(4);
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(WorkLifeBalance.CanonicalBlock + 2, balance.DynamicVars.Block.BaseValue);
        Assert.Equal(2, NewsanguoPublicApiInterop.GetDrunkenMight(Player));

        await Play(await AddToHand<KeepPlayingKeepDancing>());
        await ApplyDrunkenMight(2);
        Assert.Equal(KeepPlayingKeepDancing.BaseVigor, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(KeepPlayingKeepDancing.BaseVigor, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
        Assert.Equal(4, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Null(Player.Creature.GetPower<KeepVigorPower>());
    }

    [Fact]
    public async Task Relocate_capital_converts_vigor_and_the_drunken_might_an_attack_will_spend()
    {
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await ApplyDrunkenMight(5);
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;

        await Play(await AddToHand<RelocateCapital>(), enemy);

        Assert.Equal(hp - RelocateCapital.DamageAmount - 4 - 5, enemy.CurrentHp);
        Assert.Equal(4 + 3, Player.Creature.Block);
        Assert.Equal(4 + 3, (int)(Player.Creature.GetPower<TinderPower>()?.Amount ?? 0));
    }

    [Fact]
    public async Task Later_supreme_general_attacks_keep_the_opening_drunken_might_bonus()
    {
        await CardPileCmd.Add(PileType.Hand.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await AddCardToPile<StrikeIronclad>(PileType.Hand);
        await AddCardToPile<StrikeIronclad>(PileType.Draw);
        await ApplyDrunkenMight(4);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<Blitzkrieg>(), EnemyAt(0));

        Assert.Equal(hp - Blitzkrieg.BaseDamage - 4 - (6 + 4) - (6 + 4), EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Xiliang_savage_keeps_vigor_and_drunken_might_on_the_card()
    {
        await Play(await AddToHand<XiliangSavageScript>());
        await ApplyDrunkenMight(4);
        var strike = await AddToHand<StrikeIronclad>();

        await Play(strike, EnemyAt(0));

        Assert.Equal(6 + XiliangSavageScript.BaseVigor + 4, strike.DynamicVars.Damage.BaseValue);
    }

    [Fact]
    public async Task Vigor_amplification_adds_another_copy_of_drunken_might_and_is_spent()
    {
        await ApplyPower<VigorAmplificationPower>(Player.Creature, 1);
        await ApplyDrunkenMight(4);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(hp - 6 - 4 - 4, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
        Assert.Equal(2, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
    }

    [Fact]
    public async Task One_attack_spends_one_keep_stack_for_both_vigor_and_drunken_might()
    {
        await ApplyPower<KeepVigorPower>(Player.Creature, 2);
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await ApplyDrunkenMight(4);

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(4, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
        Assert.Equal(4, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Equal(1, (int)(Player.Creature.GetPower<KeepVigorPower>()?.Amount ?? 0));

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(4, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
        Assert.Equal(4, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Null(Player.Creature.GetPower<KeepVigorPower>());
    }

    [Fact]
    public async Task One_attack_doubles_both_resources_and_spends_the_amplification_once()
    {
        await ApplyPower<VigorAmplificationPower>(Player.Creature, 2);
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await ApplyDrunkenMight(4);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(hp - 6 - 4 - 8 - 4 - 8, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        Assert.Equal(2, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
    }

    private Task ApplyDrunkenMight(int amount) =>
        NewsanguoPublicApiInterop.ApplyDrunkenMight(
            new BlockingPlayerChoiceContext(),
            Player.Creature,
            amount,
            Player.Creature,
            null);

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

		var balance = await AddToHand<WorkLifeBalance>();
		string orText = SquCommonL10n.NewsanguoDrunkenMightOrText().GetFormattedText();
		string effectHover = SquCommonL10n.NewsanguoDrunkenMightEffectHover().GetFormattedText();
		Assert.DoesNotContain(orText, balance.GetDescriptionForPile(PileType.Hand));
		Assert.DoesNotContain(
			balance.HoverTips.OfType<HoverTip>(),
			tip => tip.Description == effectHover);

		await NewsanguoPublicApiInterop.ApplyDrunkenMight(
			new BlockingPlayerChoiceContext(),
			Player.Creature,
			1,
			Player.Creature,
			null);
		Assert.Contains(orText, balance.GetDescriptionForPile(PileType.Hand));
		Assert.DoesNotContain(
			balance.HoverTips.OfType<HoverTip>(),
			tip => tip.Description == effectHover);
	}
}
