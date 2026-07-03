using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 生死不明（The Cat Died）：随机施加灾厄的技能牌。
/// 若目标灾厄斩杀线（血条绿色部分）不大于伤害上限，则直接击杀。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "the_cat_died")]
public sealed class TheCatDied : ModCardTemplate
{
	public const string MinDamageVarName = "MinDamage";
	public const string MaxDamageVarName = "MaxDamage";

	public const int BaseMinDamage = 7;
	public const int BaseMaxDamage = 14;
	public const int UpgradedMaxDamage = 21;

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
		HoverTipFactory.FromKeyword(SquKeywords.DoomKillThreshold),
		HoverTipFactory.FromPower<DoomPower>(),
	];

	public TheCatDied()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		Creature target = cardPlay.Target;

		int maxRoll = GetMaxRoll();
		if (SquDoomKillThreshold.GetEffectiveGreenHp(target) <= maxRoll)
		{
			await DoomPower.DoomKill([target]);
			return;
		}

		int rolled = Owner.RunState.Rng.CombatTargets.NextInt(GetMinRoll(), maxRoll + 1);
		await PowerCmd.Apply<DoomPower>(
			choiceContext,
			target,
			rolled,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[MaxDamageVarName].UpgradeValueBy(UpgradedMaxDamage - BaseMaxDamage);
	}

	private int GetMinRoll() =>
		(int)DynamicVars[MinDamageVarName].BaseValue;

	private int GetMaxRoll() =>
		(int)DynamicVars[MaxDamageVarName].BaseValue;
}
