using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GoldenUprisingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("golden-uprising");

    [Fact]
    public async Task Upgrade_spends_gold_and_moves_three_selected_draw_cards_to_hand()
    {
        var first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var second = await AddCardToPile<DefendIronclad>(PileType.Draw);
        var third = await AddCardToPile<Bash>(PileType.Draw);
        int gold = Player.Gold;
        var selector = new ScriptedCardSelector().ThenSelect(first, second, third);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GoldenUprising>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(gold - GoldenUprising.BaseGoldCost, Player.Gold);
        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        AssertInPile(third, PileType.Hand);
    }

    [Fact]
    public async Task Empty_draw_pile_does_not_open_selection_or_spend_gold()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        int gold = Player.Gold;

        await Play(await AddToHand<GoldenUprising>());

        Assert.Equal(gold, Player.Gold);
    }

    [Fact]
    public async Task Less_than_required_gold_makes_card_unplayable()
    {
        await PlayerCmd.LoseGold(Player.Gold, Player, GoldLossType.Spent);
        var card = await AddToHand<GoldenUprising>();

        bool canPlay = card.CanPlay(out UnplayableReason reason, out _);

        Assert.False(canPlay);
        Assert.NotEqual(UnplayableReason.None, reason);
    }

    [Fact]
    public async Task Selection_count_is_capped_by_the_single_open_hand_slot()
    {
        while (PileType.Hand.GetPile(Player).Cards.Count < CardPile.MaxCardsInHand - 1)
        {
            await AddCardToPile<DefendIronclad>(PileType.Hand);
        }
        var first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var second = await AddCardToPile<Bash>(PileType.Draw);
        var selector = new ScriptedCardSelector().ThenSelect(first);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GoldenUprising>()));
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Draw);
        Assert.Equal(CardPile.MaxCardsInHand, PileType.Hand.GetPile(Player).Cards.Count);
    }
}
