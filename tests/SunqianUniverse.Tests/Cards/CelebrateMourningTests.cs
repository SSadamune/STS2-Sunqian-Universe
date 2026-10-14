using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CelebrateMourningTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("celebrate-mourning");

    [Fact]
    public async Task Exhausting_a_highest_rate_family_card_returns_celebrate_mourning_to_hand()
    {
        var celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        AssertInPile(celebrate, PileType.Discard);
        var family = await AddToHand<CutePet>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), family);

        AssertInPile(family, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Upgrade_grants_six_vigor_at_zero_cost()
    {
        int energy = Player.PlayerCombatState!.Energy;
        await Play(Upgrade(await AddToHand<CelebrateMourning>()));

        Assert.Equal(energy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, CelebrateMourning.UpgradedVigor);
    }

    [Fact]
    public async Task Exhausting_a_family_card_created_by_never_had_these_returns_celebrate_mourning()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        AssertInPile(celebrate, PileType.Discard);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<NeverHadThese>());
        }

        AssertAllSelectionsConsumed(selector);
        BelovedPartner family = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<BelovedPartner>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<CutePet>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<NextOfKin>());

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), family);

        AssertInPile(family, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Highest_partial_deck_play_rate_card_does_not_return_celebrate_mourning_while_family_remains()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        CardModel strike = await CreateHighestPartialPlayRateStrikeAsync();
        await PlayerCmd.SetEnergy(1, Player);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<NeverHadThese>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Contains(
            LivingFamilyCards(),
            card => card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Play);
        Assert.False(IsCurrentHighestPlayRate(strike));

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), strike);

        AssertInPile(strike, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Discard);
        Assert.Contains(LivingFamilyCards(), card => card.Pile?.Type != PileType.Exhaust);
    }

    private async Task<CardModel> CreateHighestPartialPlayRateStrikeAsync()
    {
        CardModel strike = PileType.Hand.GetPile(Player).Cards
            .Concat(PileType.Draw.GetPile(Player).Cards)
            .Concat(PileType.Discard.GetPile(Player).Cards)
            .OfType<StrikeIronclad>()
            .First(card => card.DeckVersion is not null);

        if (strike.Pile?.Type != PileType.Discard)
        {
            await CardPileCmd.Add(strike, PileType.Discard);
        }

        await CardPileCmd.Add(strike, PileType.Hand);
        await PlayerCmd.SetEnergy(1, Player);
        await Play(strike, EnemyAt(0));
        await CardPileCmd.Add(strike, PileType.Hand);

        Assert.True(CardDrawPlayRateTracker.TryGetStats(
            Player,
            strike,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true,
            out int plays,
            out int entries));
        Assert.True(plays > 0 && plays < entries);
        Assert.True(CardDrawPlayRateTracker.IsAmongHighestPlayRateDeckCards(
            Player,
            strike,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true));
        return strike;
    }

    private bool IsCurrentHighestPlayRate(CardModel card)
    {
        List<CardModel> candidates = PileType.Deck.GetPile(Player).Cards
            .Where(candidate => candidate is not CelebrateMourning)
            .ToList();
        candidates.AddRange(LivingFamilyCards());
        return CardDrawPlayRateTracker.IsAmongHighestPlayRateCards(
            Player,
            card,
            candidates,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true);
    }

    private IEnumerable<CardModel> LivingFamilyCards() =>
        Player.PlayerCombatState!.AllCards.Where(card =>
            card.DeckVersion is null
            && card is FamilyCardTemplate
            && card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Play);
}
