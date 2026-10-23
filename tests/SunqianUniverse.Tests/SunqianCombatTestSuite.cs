using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests;

/// <summary>
/// Shared helpers for Sunqian Universe combat tests.  Every helper goes through
/// the game's commands and waits for the action queue instead of mutating piles
/// directly, so tests exercise the same hooks as normal combat.
/// </summary>
public abstract class SunqianCombatTestSuite : CombatTestSuite
{
    protected static IDisposable UseCardSelector(ScriptedCardSelector selector) =>
        CardSelectCmd.UseSelector(selector);

    protected static void AssertAllSelectionsConsumed(ScriptedCardSelector selector) =>
        Assert.Equal(0, selector.PendingSteps);

    protected async Task PlayAndActivateScript(ScriptCardTemplate card)
    {
        Type recordType = card.CreateScriptRecordPreview().GetType();
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(candidate => candidate.GetType() == recordType);
        using (UseCardSelector(selector))
        {
            await Play(card);
        }
        AssertAllSelectionsConsumed(selector);
    }

    protected async Task PlayAndActivateScript(ScriptCardTemplate card, Creature target)
    {
        Type recordType = card.CreateScriptRecordPreview().GetType();
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(candidate => candidate.GetType() == recordType);
        using (UseCardSelector(selector))
        {
            await Play(card, target);
        }
        AssertAllSelectionsConsumed(selector);
    }

    protected async Task<TCard> AddCardToPile<TCard>(
        PileType pileType,
        CardPilePosition position = CardPilePosition.Bottom,
        Player? owner = null)
        where TCard : CardModel
    {
        owner ??= Player;
        var card = Combat.CreateCard<TCard>(owner);

        await CardPileCmd.AddGeneratedCardToCombat(card, pileType, owner, position);
        await WaitFor(
            () => card.Pile?.Type == pileType,
            $"Injected {typeof(TCard).Name} did not appear in {pileType}.");
        await WaitForIdle();

        return card;
    }

    protected async Task<CardModel> AddCardToPile(
        ModelId cardId,
        PileType pileType,
        CardPilePosition position = CardPilePosition.Bottom,
        Player? owner = null)
    {
        owner ??= Player;
        CardModel canonical = ModelDb.GetById<CardModel>(cardId);
        CardModel card = owner.Creature.CombatState!.CreateCard(canonical, owner);

        await CardPileCmd.AddGeneratedCardToCombat(card, pileType, owner, position);
        await WaitFor(
            () => card.Pile?.Type == pileType,
            $"Injected {cardId} did not appear in {pileType}.");
        await WaitForIdle();

        return card;
    }

    protected Task<TCard> AddToDrawPileTop<TCard>(Player? owner = null)
        where TCard : CardModel =>
        AddCardToPile<TCard>(PileType.Draw, CardPilePosition.Top, owner);

    protected Task<CardModel> AddToHand(ModelId cardId, Player? owner = null) =>
        AddCardToPile(cardId, PileType.Hand, CardPilePosition.Bottom, owner);

    protected static TCard Upgrade<TCard>(TCard card)
        where TCard : CardModel
    {
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        return card;
    }

    protected static TCard GiveSingleTurnRetain<TCard>(TCard card)
        where TCard : CardModel
    {
        card.GiveSingleTurnRetain();
        return card;
    }

    protected static void AssertInPile(CardModel card, PileType expected) =>
        Assert.Equal(expected, card.Pile?.Type);

    protected void AssertOnlyScript<TScript>()
        where TScript : ScriptPowerTemplate
    {
        ScriptPowerTemplate script = Assert.Single(
            Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        Assert.IsType<TScript>(script);
    }
}
