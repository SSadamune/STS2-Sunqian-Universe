using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Random;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 三叔四伯：从抽牌堆和弃牌堆按不同能量花费各取一张入手牌，并在本回合打乱这些非 X 费牌的花费。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "third_uncle_fourth_uncle")]
public sealed class ThirdUncleFourthUncle : ModCardTemplate
{
	private const int XCostGroup = int.MaxValue;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ThirdUncleFourthUncle.png");

	public ThirdUncleFourthUncle()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> moved = await MoveDistinctCostCardsToHand();
		ShuffleCostsThisTurn(moved.Where(card => !card.EnergyCost.CostsX).ToList());
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
	}

	private async Task<List<CardModel>> MoveDistinctCostCardsToHand()
	{
		List<CardModel> pool =
		[
			..PileType.Draw.GetPile(Owner).Cards,
			..PileType.Discard.GetPile(Owner).Cards,
		];
		Owner.RunState.Rng.CombatCardGeneration.Shuffle(pool);

		List<CardModel> chosen = [];
		HashSet<int> seenCosts = [];
		foreach (CardModel card in pool)
		{
			if (!TryGetCostGroup(card, out int costGroup) || !seenCosts.Add(costGroup))
			{
				continue;
			}

			chosen.Add(card);
		}

		chosen.Sort((left, right) => CostGroup(left).CompareTo(CostGroup(right)));

		int handSpace = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
		if (chosen.Count > handSpace)
		{
			chosen.RemoveRange(handSpace, chosen.Count - handSpace);
		}

		foreach (CardModel card in chosen)
		{
			await CardPileCmd.Add(card, PileType.Hand);
		}

		return chosen;
	}

	private void ShuffleCostsThisTurn(List<CardModel> cards)
	{
		if (cards.Count < 2)
		{
			return;
		}

		int[] costs = cards.Select(card => card.EnergyCost.GetWithModifiers(CostModifiers.Local)).ToArray();
		int[] sources = DerangeIndices(cards.Count, Owner.RunState.Rng.CombatEnergyCosts);
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel card = cards[i];
			card.EnergyCost.SetThisTurnOrUntilPlayed(costs[sources[i]]);
			NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
		}
	}

	/// <summary>
	/// Sattolo 洗牌：生成一个随机 n 循环，因此 n ≥ 2 时每张牌都会换成另一张牌的花费。
	/// </summary>
	private static int[] DerangeIndices(int count, Rng rng)
	{
		int[] order = new int[count];
		for (int i = 0; i < count; i++)
		{
			order[i] = i;
		}

		for (int i = count - 1; i >= 1; i--)
		{
			int swapWith = rng.NextInt(i);
			(order[i], order[swapWith]) = (order[swapWith], order[i]);
		}

		return order;
	}

	private static bool TryGetCostGroup(CardModel card, out int costGroup)
	{
		if (card.EnergyCost.CostsX)
		{
			costGroup = XCostGroup;
			return true;
		}

		int cost = card.EnergyCost.GetWithModifiers(CostModifiers.Local);
		if (cost < 0)
		{
			costGroup = 0;
			return false;
		}

		costGroup = cost;
		return true;
	}

	private static int CostGroup(CardModel card) =>
		TryGetCostGroup(card, out int costGroup) ? costGroup : 0;
}
