using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 无双乱舞：对所有敌人造成伤害，并按敏捷的 3（升级 4）倍增加该伤害。
/// 卡面伤害预览包含敏捷加成，印面 BaseValue 不变以便 {Damage:diff()} 标绿。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "musou_attack")]
public sealed class MusouAttack : ModCardTemplate
{
	public const decimal CanonicalDamage = 17m;
	public const decimal UpgradedDamage = 21m;
	public const decimal CanonicalDexterityMultiplier = 3m;
	public const decimal UpgradedDexterityMultiplier = 4m;

	public const string DexterityMultVarName = "DexterityMult";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DexterityScaledDamageVar(CanonicalDamage),
		new DynamicVar(DexterityMultVarName, CanonicalDexterityMultiplier),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/MusouAttack.png");

	protected override bool ShouldGlowGoldInternal => GetDexterityBonus(this) != 0m;

	public MusouAttack()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(CombatState, nameof(CombatState));

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue + GetDexterityBonus(this))
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(CombatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
		DynamicVars[DexterityMultVarName]
			.UpgradeValueBy(UpgradedDexterityMultiplier - CanonicalDexterityMultiplier);
	}

	internal static decimal GetDexterityBonus(CardModel card)
	{
		if (card.Owner?.Creature is not Creature owner)
		{
			return 0m;
		}

		decimal dexterity = owner.GetPower<DexterityPower>()?.Amount ?? 0m;
		if (dexterity == 0m)
		{
			return 0m;
		}

		decimal multiplier = card.DynamicVars.ContainsKey(DexterityMultVarName)
			? card.DynamicVars[DexterityMultVarName].BaseValue
			: CanonicalDexterityMultiplier;
		return dexterity * multiplier;
	}

	/// <summary>
	/// 卡面伤害预览包含当前敏捷加成；<see cref="DynamicVar.BaseValue"/> 保持印面数值，
	/// 这样 <c>{Damage:diff()}</c> 才能把加成后的数字标绿。
	/// </summary>
	private sealed class DexterityScaledDamageVar : DamageVar
	{
		public DexterityScaledDamageVar(decimal baseValue)
			: base(baseValue, ValueProp.Move)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal amount = BaseValue + GetDexterityBonus(card);
			if (!runGlobalHooks || card.CombatState is not { } combatState)
			{
				PreviewValue = amount;
				return;
			}

			PreviewValue = Math.Max(
				0m,
				Hook.ModifyDamage(
					card.Owner.RunState,
					combatState,
					target,
					card.Owner.Creature,
					amount,
					Props,
					card,
					null,
					ModifyDamageHookType.All,
					previewMode,
					out _));
		}
	}
}
