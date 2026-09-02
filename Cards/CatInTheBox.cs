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
using MegaCrit.Sts2.Core.Models.Powers;
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
/// 若目标灾厄斩杀线（血条绿色部分）不大于灾厄上限，则直接击杀。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "cat_in_the_box")]
public sealed class CatInTheBox : ModCardTemplate
{
	public const string MinDoomVarName = "MinDoom";
	public const string MaxDoomVarName = "MaxDoom";
	private const string CanKillVarName = "CanKill";

	public const int BaseMinDoom = 7;
	public const int BaseMaxDoom = 14;
	public const int UpgradedMinDoom = 12;
	public const int UpgradedMaxDoom = 24;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		ModCardVars.Int(MinDoomVarName, BaseMinDoom),
		ModCardVars.Int(MaxDoomVarName, BaseMaxDoom),
		ModCardVars.Computed(CanKillVarName, 0,
			(CardModel? card, Creature? target) =>
				card is CatInTheBox s && target != null &&
				SquDoomKillThreshold.GetEffectiveGreenHp(target) <= s.GetMaxRoll() ? 1 : 0),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CatInTheBox.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.DoomKillThreshold),
		HoverTipFactory.FromPower<DoomPower>(),
	];

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			ICombatState? combatState = CombatState;
			if (combatState == null)
			{
				return false;
			}

			int maxRoll = GetMaxRoll();
			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (enemy.IsAlive && SquDoomKillThreshold.GetEffectiveGreenHp(enemy) <= maxRoll)
				{
					return true;
				}
			}

			return false;
		}
	}

	public CatInTheBox()
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
		DynamicVars[MinDoomVarName].UpgradeValueBy(UpgradedMinDoom - BaseMinDoom);
		DynamicVars[MaxDoomVarName].UpgradeValueBy(UpgradedMaxDoom - BaseMaxDoom);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		if (DynamicVars[CanKillVarName].PreviewValue > 0)
		{
			description.Add("BodyText", new LocString("cards", Id.Entry + ".killConfirm"));
			return;
		}

		var bodyText = new LocString("cards", Id.Entry + ".normalBody");
		bodyText.Add(DynamicVars[MinDoomVarName]);
		bodyText.Add(DynamicVars[MaxDoomVarName]);
		description.Add("BodyText", bodyText);
	}

	private int GetMinRoll() =>
		(int)DynamicVars[MinDoomVarName].BaseValue;

	private int GetMaxRoll() =>
		(int)DynamicVars[MaxDoomVarName].BaseValue;
}
