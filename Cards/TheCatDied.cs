using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>
/// 生死不明（The Cat Died）：随机伤害攻击牌。
/// 若目标生命不大于经全部修正后的伤害上限，则直接击杀。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "the_cat_died")]
public sealed class TheCatDied : ModCardTemplate
{
	public const string MinDamageVarName = "MinDamage";
	public const string MaxDamageVarName = "MaxDamage";

	public const int BaseMinDamage = 6;
	public const int BaseMaxDamage = 14;
	public const int UpgradedMaxDamage = 20;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		ModCardVars.ComputedDamage(
			MinDamageVarName,
			BaseMinDamage,
			(card, _) => card is TheCatDied s ? s.GetMinRoll() : BaseMinDamage,
			ValueProp.Move),
		ModCardVars.ComputedDamage(
			MaxDamageVarName,
			BaseMaxDamage,
			(card, _) => card is TheCatDied s ? s.GetMaxRoll() : BaseMaxDamage,
			ValueProp.Move),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TheCatDied.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		CreateDoomKillSupplementHoverTip(),
		HoverTipFactory.FromPower<DoomPower>(),
	];

	public TheCatDied()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		Creature target = cardPlay.Target;

		decimal damageCeiling = CalculateDamageCeiling(this, target);
		if (target.CurrentHp <= damageCeiling)
		{
			// 不走 DamageCmd / CreatureCmd.Damage，避免对已算入修正的上限再套一层 ModifyDamage。
			await DoomPower.DoomKill([target]);
			return;
		}

		int rolled = Owner.RunState.Rng.CombatTargets.NextInt(GetMinRoll(), GetMaxRoll() + 1);
		await DamageCmd.Attack(rolled)
			.FromCard(this)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[MaxDamageVarName].UpgradeValueBy(UpgradedMaxDamage - BaseMaxDamage);
	}

	private int GetMinRoll() =>
		(int)DynamicVars[MinDamageVarName].BaseValue;

	private int GetMaxRoll() =>
		(int)DynamicVars[MaxDamageVarName].BaseValue;

	private static IHoverTip CreateDoomKillSupplementHoverTip() =>
		new HoverTip(
			SquCommonL10n.AnnotationTitle(),
			new LocString("cards", "SUNQIAN_UNIVERSE_CARD_THE_CAT_DIED.doomKillSupplement.description"));

	public static decimal CalculateDamageCeiling(CardModel card, Creature target)
	{
		int maxRoll = card is TheCatDied theCatDied
			? theCatDied.GetMaxRoll()
			: BaseMaxDamage;

		return Hook.ModifyDamage(
			card.Owner.RunState,
			card.CombatState,
			target,
			card.Owner.Creature,
			maxRoll,
			ValueProp.Move,
			card,
			ModifyDamageHookType.All,
			CardPreviewMode.None,
			out _);
	}
}
