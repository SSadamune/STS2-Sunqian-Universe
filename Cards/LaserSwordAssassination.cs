using System;
using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 光剑刺杀：可升级三次。第二次升级后改为穿透伤害，第三次升级后获得保留；
/// 若目标意图不是攻击，则在力量、活力等加成之后将伤害变为两倍或三倍。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "laser_sword_assassination")]
public sealed class LaserSwordAssassination : ModCardTemplate, IPenetratingDamageCard
{
	public const int CanonicalDamage = 4;
	public const int MaximumUpgradeLevel = 3;
	public const int CanonicalDamageMultiplier = 2;
	public const int UpgradedDamageMultiplier = 3;
	private const string IsAmplifiedVarName = "IsAmplified";

	private bool IsUpgradePreview => UpgradePreviewType != CardUpgradePreviewType.None;

	internal static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	public override int MaxUpgradeLevel => MaximumUpgradeLevel;

	public override string Title =>
		TitleLocString.GetFormattedText() + new string('+', CurrentUpgradeLevel);

	public bool DealsPenetratingDamage => CurrentUpgradeLevel >= 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, DamageProps),
		ModCardVars.Computed(
			IsAmplifiedVarName,
			0m,
			(CardModel? card, Creature? target) => ShouldAmplifyDamage(target) ? 1m : 0m),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			if (DealsPenetratingDamage)
			{
				yield return HoverTipFactory.FromKeyword(SquKeywords.PiercingDamage);
			}

			if (IsUpgradePreview && CurrentUpgradeLevel >= MaximumUpgradeLevel)
			{
				yield return HoverTipFactory.FromKeyword(CardKeyword.Retain);
			}

			if (IsUpgradePreview
				|| CombatManager.Instance.IsInProgress
				|| CurrentUpgradeLevel >= MaximumUpgradeLevel)
			{
				yield break;
			}

			for (int level = CurrentUpgradeLevel + 1; level <= MaximumUpgradeLevel; level++)
			{
				LaserSwordAssassination preview = CreateUpgradePreview(level);
				yield return new UpgradeLevelCardHoverTip(preview);
				foreach (IHoverTip hoverTip in preview.HoverTips)
				{
					if (hoverTip is CardHoverTip)
					{
						continue;
					}

					yield return hoverTip;
				}
			}
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LaserSwordAssassination.png");

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			ICombatState? combatState = CombatState;
			if (combatState == null)
			{
				return false;
			}

			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (enemy.IsAlive && !SquEnemyIntent.IntendsToAttack(enemy))
				{
					return true;
				}
			}

			return false;
		}
	}

	public LaserSwordAssassination()
		: base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		SquSfx.Play(ShouldAmplifyDamage(cardPlay.Target)
			? SquSfx.LaserSwordAssassinationLaserEvent
			: SquSfx.LaserSwordAssassinationDrawEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
		if (CurrentUpgradeLevel == MaximumUpgradeLevel)
		{
			AddKeyword(CardKeyword.Retain);
		}
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		bool amplified = DynamicVars[IsAmplifiedVarName].PreviewValue > 0m;
		bool tripleDamage = CurrentDamageMultiplier == UpgradedDamageMultiplier;
		string bodyKey = (amplified, tripleDamage) switch
		{
			(true, true) => Id.Entry + ".amplifiedTripleBody",
			(true, false) => Id.Entry + ".amplifiedDoubleBody",
			(false, true) => Id.Entry + ".normalTripleBody",
			_ => Id.Entry + ".normalDoubleBody",
		};
		var body = new LocString("cards", bodyKey);
		body.Add(DynamicVars.Damage);
		bool highlightRetain = IsUpgradePreview && CurrentUpgradeLevel == MaximumUpgradeLevel;
		string damageTypeKey = DealsPenetratingDamage && IsUpgradePreview && CurrentUpgradeLevel == 2
			? ".piercingDamagePreview"
			: DealsPenetratingDamage
				? ".piercingDamage"
				: ".unblockableDamage";
		body.Add("DamageType", new LocString("cards", Id.Entry + damageTypeKey).GetFormattedText());
		body.Add("TripleWord", PreviewText(".tripleWord", CurrentUpgradeLevel == 1));
		body.Add("TripledWord", PreviewText(".tripledWord", CurrentUpgradeLevel == 1));
		if (highlightRetain)
		{
			RemoveKeyword(CardKeyword.Retain);
		}

		body.Add(
			"RetainPreviewLine",
			highlightRetain
				? new LocString("cards", Id.Entry + ".retainPreviewLine").GetFormattedText()
				: string.Empty);
		body.Add("RemainingUpgradesText", GetRemainingUpgradesText());
		description.Add("BodyText", body);
	}

	private string GetRemainingUpgradesText()
	{
		int remainingUpgrades = MaxUpgradeLevel - CurrentUpgradeLevel;
		if (remainingUpgrades <= 0)
		{
			return string.Empty;
		}

		var text = new LocString("cards", Id.Entry + ".remainingUpgrades");
		text.Add("Remaining", remainingUpgrades);
		return text.GetFormattedText();
	}

	internal static bool ShouldAmplifyDamage(Creature? target) =>
		target is { IsAlive: true } && !SquEnemyIntent.IntendsToAttack(target);

	/// <summary>
	/// 让意图倍率参与 Hook.ModifyDamage 的乘算阶段，使力量、活力等先参与计算。
	/// </summary>
	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (cardSource != this
			|| !props.IsPoweredAttack()
			|| !ShouldAmplifyDamage(target))
		{
			return 1m;
		}

		return CurrentDamageMultiplier;
	}

	private int CurrentDamageMultiplier => CurrentUpgradeLevel >= 1
		? UpgradedDamageMultiplier
		: CanonicalDamageMultiplier;

	private string PreviewText(string keyStem, bool highlight)
	{
		string suffix = IsUpgradePreview && highlight ? "Preview" : string.Empty;
		return new LocString("cards", Id.Entry + keyStem + suffix).GetFormattedText();
	}

	private LaserSwordAssassination CreateUpgradePreview(int targetLevel)
	{
		var preview = (LaserSwordAssassination)MutableClone();
		preview.UpgradePreviewType = CardUpgradePreviewType.Deck;
		while (preview.CurrentUpgradeLevel < targetLevel)
		{
			preview.UpgradeInternal();
		}

		return preview;
	}

	/// <summary>
	/// 原版 <see cref="CardHoverTip"/> 只要升级过，编号都只加一个加号，多级卡面会互相顶掉。
	/// </summary>
	private sealed class UpgradeLevelCardHoverTip : CardHoverTip, IHoverTip
	{
		private readonly string _id;

		public UpgradeLevelCardHoverTip(LaserSwordAssassination card)
			: base(card)
		{
			_id = $"{card.Id.Entry}+{card.CurrentUpgradeLevel}";
		}

		string IHoverTip.Id => _id;
	}
}
