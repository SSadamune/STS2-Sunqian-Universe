using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 闻丧贺喜：获得活力与格挡并抽牌；牌组中打出率最高的「其它」牌被消耗时回手。
/// 升级后回手时本场战斗耗能改为 0。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "celebrate_mourning")]
public sealed class CelebrateMourning : ModCardTemplate
{
	public const int VigorAmount = 4;

	public const int BlockAmount = 4;

	public const int DrawAmount = 1;

	public const int PlayRateWindow = CardDrawPlayRateTracker.MaxStoredCombats;

	private const bool IncludeCurrentCombat = true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(VigorAmount),
		new BlockVar(BlockAmount, ValueProp.Move),
		new CardsVar(DrawAmount),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			List<IHoverTip> tips = [HoverTipFactory.FromPower<VigorPower>()];
			List<CardModel> triggerTargets = GetReturnTriggerTargets();
			IHoverTip? triggerTip = CreateReturnTriggerHoverTip(triggerTargets);
			if (triggerTip != null)
			{
				tips.Add(triggerTip);
				foreach (CardModel target in triggerTargets)
				{
					tips.Add(HoverTipFactory.FromCard(target));
					tips.AddRange(target.HoverTips);
				}
			}

			return tips;
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CelebrateMourning.png");

	public CelebrateMourning()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	/// <summary>
	/// 任意卡牌被消耗后：若其为牌组中打出率最高的「其它」牌（并列任一），则本牌回手。
	/// </summary>
	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (!CanReturnToHand() || card.Owner != Owner || IsCelebrateMourning(card))
		{
			return;
		}

		CardModel? identity = card.DeckVersion ?? (card.Pile?.Type == PileType.Deck ? card : null);
		if (identity is null || IsCelebrateMourning(identity))
		{
			return;
		}

		HashSet<CardModel> highest = GetHighestOtherPlayRateDeckCards();
		if (!highest.Contains(identity))
		{
			return;
		}

		CardDrawPlayRateTracker.LogCurrentState(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat,
			selectedCards: highest.ToList(),
			reason: $"Celebrate Mourning return-to-hand (exhausted: {identity.Title})");

		if (IsUpgraded)
		{
			EnergyCost.SetThisCombat(0);
		}

		await CardPileCmd.Add(this, PileType.Hand);
	}

	private List<CardModel> GetReturnTriggerTargets()
	{
		// 图鉴规范卡不可变，访问 Owner 会 AssertMutable；仅可变且绑定运行的预览才显示动态目标。
		if (RunState is null || !IsMutable)
		{
			return [];
		}

		return GetHighestOtherPlayRateDeckCards()
			.OrderBy(card => card.Title, System.StringComparer.Ordinal)
			.ToList();
	}

	private IHoverTip? CreateReturnTriggerHoverTip(IReadOnlyList<CardModel> triggerTargets)
	{
		if (triggerTargets.Count == 0)
		{
			return null;
		}

		LocString description = new("cards", Id.Entry + ".returnTriggerHoverTip");
		description.Add("Cards", FormatCardList(triggerTargets));
		return new HoverTip(SquCommonL10n.AnnotationTitle(), description);
	}

	private HashSet<CardModel> GetHighestOtherPlayRateDeckCards() =>
		CardDrawPlayRateTracker.GetHighestPlayRateDeckCards(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat,
			exclude: IsCelebrateMourning);

	private string FormatCardList(IReadOnlyList<CardModel> cards)
	{
		string separator = new LocString("cards", Id.Entry + ".cardSeparator").GetFormattedText()
			?? ", ";
		return string.Join(separator, cards.Select(card => $"[gold]{card.Title}[/gold]"));
	}

	private static bool IsCelebrateMourning(CardModel card) =>
		card is CelebrateMourning || card.DeckVersion is CelebrateMourning;

	private static bool CanReturnToHand(PileType? pileType) =>
		pileType is PileType.Draw or PileType.Discard or PileType.Exhaust;

	private bool CanReturnToHand() => CanReturnToHand(Pile?.Type);
}
