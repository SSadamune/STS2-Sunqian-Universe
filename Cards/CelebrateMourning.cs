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
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 闻丧贺喜：获得活力；被消耗的牌不低于手牌、抽牌堆和弃牌堆中的最高打出率时回手。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "celebrate_mourning")]
public sealed class CelebrateMourning : ModCardTemplate
{
	public const int BaseVigor = 5;

	public const int UpgradedVigor = 7;

	public const int PlayRateWindow = CardDrawPlayRateTracker.MaxStoredCombats;

	private const bool IncludeCurrentCombat = true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			List<IHoverTip> tips =
			[
				HoverTipFactory.FromPower<VigorPower>(),
				SquCommonL10n.PlayRateHoverTip(),
			];
			List<CardModel> triggerTargets = GetReturnTriggerTargets();
			IHoverTip? triggerTip = CreateReturnTriggerHoverTip(triggerTargets);
			if (triggerTip != null)
			{
				tips.Add(triggerTip);
				foreach (CardModel target in triggerTargets)
				{
					tips.Add(HoverTipFactory.FromCard(target));
				}
			}

			return tips;
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CelebrateMourning.png");

	public CelebrateMourning()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.CelebrateMourningCongratulateLordEvent);
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
	}

	/// <summary>
	/// 任意实体卡牌被消耗后：若该牌在消耗时的有效候选中打出率最高（并列任一），则本牌回手。
	/// </summary>
	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (!CanListenForReturnTrigger() || card.Owner != Owner)
		{
			return;
		}

		List<CardModel> candidates = GetPlayRateCandidates(card);
		if (!CardDrawPlayRateTracker.IsAmongHighestPlayRateCards(
			    Owner,
			    card,
			    candidates,
			    windowSize: PlayRateWindow,
			    includeCurrentCombat: IncludeCurrentCombat))
		{
			return;
		}

		HashSet<CardModel> highest = GetHighestPlayRateCards(candidates);

		CardDrawPlayRateTracker.LogCurrentState(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat,
			selectedCards: highest.ToList(),
			reason: $"Celebrate Mourning return-to-hand (exhausted: {card.Title})");

		SquSfx.Play(SquSfx.CelebrateMourningGrieveForLordEvent);
		if (Pile?.Type != PileType.Hand)
		{
			await CardPileCmd.Add(this, PileType.Hand);
		}
	}

	private List<CardModel> GetReturnTriggerTargets()
	{
		// 图鉴规范卡不可变，访问 Owner 会 AssertMutable；仅可变且绑定运行的预览才显示动态目标。
		if (RunState is null || !IsMutable)
		{
			return [];
		}

		return GetHighestPlayRateCards(GetPlayRateCandidates())
			.OrderBy(card => card.Title, System.StringComparer.Ordinal)
			.ToList();
	}

	private List<CardModel> GetPlayRateCandidates(CardModel? exhaustedCard = null)
		=> CardDrawPlayRateTracker.GetCelebrateMourningPlayRateCandidates(
			Owner,
			exhaustedCard);

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

	private HashSet<CardModel> GetHighestPlayRateCards(IEnumerable<CardModel> candidates) =>
		CardDrawPlayRateTracker.GetHighestPlayRateCards(
			Owner,
			candidates,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat);

	private string FormatCardList(IReadOnlyList<CardModel> cards)
	{
		string separator = new LocString("cards", Id.Entry + ".cardSeparator").GetFormattedText()
			?? ", ";
		return string.Join(separator, cards.Select(card => $"[gold]{card.Title}[/gold]"));
	}

	private static bool CanListenForReturnTrigger(PileType? pileType) =>
		pileType is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust;

	private bool CanListenForReturnTrigger() => CanListenForReturnTrigger(Pile?.Type);
}
