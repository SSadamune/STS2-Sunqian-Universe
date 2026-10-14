using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace Squ.Tests;

/// <summary>
/// Deterministic selector for combat tests. Each selection prompt consumes one
/// queued step, so an unexpected or reordered prompt fails the test immediately.
/// </summary>
public sealed class ScriptedCardSelector : ICardSelector
{
    private readonly Queue<Func<IReadOnlyList<CardModel>, IReadOnlyList<CardModel>>> _steps = [];

    public int PendingSteps => _steps.Count;

    public ScriptedCardSelector ThenSelect(params CardModel[] cards) =>
        ThenSelect(_ => cards);

    public ScriptedCardSelector ThenSelectWhere(params Func<CardModel, bool>[] predicates) =>
        ThenSelect(options => predicates
            .Select(predicate => options.Single(predicate))
            .ToArray());

    public ScriptedCardSelector ThenSelectFirst(int count = 1) =>
        ThenSelect(options => options.Take(count).ToArray());

    public ScriptedCardSelector ThenSkip() =>
        ThenSelect(_ => []);

    public ScriptedCardSelector ThenSelect(
        Func<IReadOnlyList<CardModel>, IReadOnlyList<CardModel>> selection)
    {
        _steps.Enqueue(selection);
        return this;
    }

    public Task<IEnumerable<CardModel>> GetSelectedCards(
        IEnumerable<CardModel> options,
        int minSelect,
        int maxSelect)
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException(
                "A card-selection prompt was shown without a queued scripted response.");
        }

        IReadOnlyList<CardModel> available = options.ToArray();
        IReadOnlyList<CardModel> selected = _steps.Dequeue()(available);
        int effectiveMinimum = Math.Min(minSelect, available.Count);
        int effectiveMaximum = Math.Min(maxSelect, available.Count);

        if (selected.Count < effectiveMinimum || selected.Count > effectiveMaximum)
        {
            throw new InvalidOperationException(
                $"Script selected {selected.Count} card(s), but prompt allows " +
                $"{effectiveMinimum}..{effectiveMaximum} from {available.Count} option(s).");
        }

        if (selected.Distinct().Count() != selected.Count ||
            selected.Any(card => !available.Contains(card)))
        {
            throw new InvalidOperationException(
                "Scripted card selection must contain distinct cards from the offered options.");
        }

        return Task.FromResult<IEnumerable<CardModel>>(selected);
    }

    public CardRewardSelection GetSelectedCardReward(
        IReadOnlyList<CardCreationResult> options,
        IReadOnlyList<CardRewardAlternative> alternatives) =>
        throw new InvalidOperationException(
            "ScriptedCardSelector does not implicitly choose combat rewards.");
}
