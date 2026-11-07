using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 复仇打击：造成基础伤害，并加上玩家在指定回合窗口内实际损失的生命。
/// 窗口从最近（升级后为倒数第二近）一次玩家回合结束开始；边界不足时
/// 从本场战斗开始统计。伤害与战斗内动态描述共用同一个失血查询。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "revenge_strike")]
public sealed class RevengeStrike : ModCardTemplate
{
	public const int BaseDamage = 7;
	public const int UpgradedDamage = 9;
	public const int BaseWindowTurns = 1;
	public const int UpgradedWindowTurns = 2;

	public const string TurnsVarName = "Turns";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new DynamicVar(TurnsVarName, BaseWindowTurns),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/prisoner/RevengeStrike.png");

	public RevengeStrike()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		int bouns = GetCurrentBouns();
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue + bouns)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars[TurnsVarName].UpgradeValueBy(UpgradedWindowTurns - BaseWindowTurns);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("bouns", CanShowBouns() ? $"(+{GetCurrentBouns()})" : string.Empty);
	}

	private int GetCurrentBouns()
	{
		if (!CanShowBouns())
		{
			return 0;
		}

		return Math.Max(0, RevengeStrikeTrackerPower.GetLostHp(
			Owner,
			DynamicVars[TurnsVarName].IntValue));
	}

	private bool CanShowBouns() =>
		IsMutable && Owner?.PlayerCombatState != null && CombatManager.Instance.IsInProgress;
}
