using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 花费金币从抽牌堆选牌入手牌（对齐 <see cref="MegaCrit.Sts2.Core.Models.Cards.Wish"/>），
/// 金币消耗对齐 <see cref="MegaCrit.Sts2.Core.Models.Relics.SealOfGold"/>。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "golden_uprising")]
public sealed class GoldenUprising : ModCardTemplate
{
	public const int BaseGoldCost = 5;
	public const int GoldEscalationPerPlay = 5;
	public const int CardPickCount = 2;

	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_GOLDEN_UPRISING.selectionScreenPrompt");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(BaseGoldCost),
		new CardsVar(CardPickCount),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GoldenUprising.png");

	public GoldenUprising()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override bool IsPlayable =>
		Owner.Gold >= DynamicVars.Gold.IntValue;

	protected override bool ShouldGlowGoldInternal => IsPlayable;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int pickCount = GetPickCount();
		if (pickCount <= 0)
		{
			return;
		}

		int goldCost = DynamicVars.Gold.IntValue;
		await PlayerCmd.LoseGold(goldCost, Owner, GoldLossType.Spent);

		IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
			choiceContext,
			PileType.Draw.GetPile(Owner),
			Owner,
			new CardSelectorPrefs(SelectionPrompt, pickCount));

		foreach (CardModel card in selected)
		{
			await CardPileCmd.Add(card, PileType.Hand);
		}

		if (IsUpgraded)
		{
			DynamicVars.Gold.UpgradeValueBy(GoldEscalationPerPlay);
		}
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
	}

	/// <summary>
	/// 在 <see cref="OnPlay"/> 内调用（本牌已离手），对齐 WISH / Dredge 的选牌上限算法。
	/// </summary>
	private int GetPickCount()
	{
		int drawCount = PileType.Draw.GetPile(Owner).Cards.Count;
		int handSpace = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
		return Math.Min(CardPickCount, Math.Min(drawCount, handSpace));
	}
}
