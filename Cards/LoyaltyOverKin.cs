using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 大义灭亲：消耗抽牌堆中打出率最高的 2 张牌
/// （PlayCount / (PlayWithoutDiscardOrExhaustCount + ExhaustEntryCount + DiscardEntryCount)；
/// 率相同则优先打出次数更多者，再按获得顺序）；每消耗一张先造成伤害再获得力量。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "loyalty_over_kin")]
public sealed class LoyaltyOverKin : ModCardTemplate
{
	public const int DamageAmount = 7;

	public const int BaseStrength = 2;

	public const int UpgradedStrength = 3;

	public const int ExhaustCount = 2;

	/// <summary>使用追踪器已保存的全部已结束战斗。</summary>
	public const int PlayRateWindow = CardDrawPlayRateTracker.MaxStoredCombats;

	private const bool IncludeCurrentCombat = true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new PowerVar<StrengthPower>(BaseStrength),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Ethereal,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LoyaltyOverKin.png");

	public LoyaltyOverKin()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		List<CardModel> selected = SelectTargets();

		CardDrawPlayRateTracker.LogCurrentState(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat,
			selectedCards: selected,
			reason: $"Loyalty Over Kin OnPlay (count={ExhaustCount})");

		Creature target = cardPlay.Target;
		decimal strengthAmount = DynamicVars[nameof(StrengthPower)].BaseValue;
		SquVigorSnapshot.AttackSequence vigorSequence = SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);

		foreach (CardModel card in selected)
		{
			if (card.Pile?.Type != PileType.Draw)
			{
				continue;
			}

			await CardCmd.Exhaust(choiceContext, card);

			if (target.IsAlive)
			{
				await DamageCmd.Attack(vigorSequence.ResolveNextAttackDamage())
					.FromCard(this, cardPlay)
					.Targeting(target)
					.WithHitFx("vfx/vfx_attack_slash")
					.Execute(choiceContext);
			}

			await PowerCmd.Apply<StrengthPower>(
				choiceContext,
				Owner.Creature,
				strengthAmount,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(StrengthPower)].UpgradeValueBy(UpgradedStrength - BaseStrength);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		List<CardModel> previewTargets = GetDescriptionPreviewTargets();
		if (previewTargets.Count == 0)
		{
			description.Add("TargetCards", string.Empty);
			return;
		}

		var clause = new LocString("cards", Id.Entry + ".targetCards");
		clause.Add("Cards", FormatCardList(previewTargets));
		description.Add("TargetCards", clause);
	}

	private List<CardModel> GetDescriptionPreviewTargets()
	{
		// 图鉴规范卡不可变，访问 Owner 会 AssertMutable；仅战斗中的可变实例才预览消耗目标。
		if (!IsMutable || RunState is null || !CombatManager.Instance.IsInProgress || Owner?.PlayerCombatState == null)
		{
			return [];
		}

		return SelectTargets();
	}

	private List<CardModel> SelectTargets() =>
		CardDrawPlayRateTracker.SelectHighestPlayRateFromDrawPile(
			Owner,
			ExhaustCount,
			windowSize: PlayRateWindow,
			includeCurrentCombat: IncludeCurrentCombat);

	private string FormatCardList(IReadOnlyList<CardModel> cards)
	{
		string separator = new LocString("cards", Id.Entry + ".cardSeparator").GetFormattedText()
			?? ", ";
		return string.Join(separator, cards.Select(card => $"[gold]{card.Title}[/gold]"));
	}
}
