using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "respect_elders")]
public sealed class RespectElders : ModCardTemplate
{
	private const int BaseHitCount = 2;
	private const int UpgradedHitCount = 3;
	private const string OutOfRangeVarName = "OutOfRange";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(7m, ValueProp.Move),
		new DynamicVar("HitCount", BaseHitCount),
		ModCardVars.Computed(OutOfRangeVarName, 0,
			(CardModel? card, Creature? target) =>
				target != null && !IsInHpRange(target) ? 1 : 0),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/RespectElders.png");

	protected override bool ShouldGlowGoldInternal =>
		CombatState?.HittableEnemies.Any(IsInHpRange) ?? false;

	public RespectElders()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		int hitCount = IsInHpRange(cardPlay.Target)
			? (int)DynamicVars["HitCount"].BaseValue
			: 1;

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(hitCount)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars["HitCount"].UpgradeValueBy(UpgradedHitCount - BaseHitCount);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		bool outOfRange = DynamicVars[OutOfRangeVarName].PreviewValue > 0;
		string locKey = Id.Entry + (outOfRange ? ".noHitCondition" : ".hitCondition");
		var hitText = new LocString("cards", locKey);
		if (!outOfRange)
		{
			hitText.Add(DynamicVars["HitCount"]);
		}
		description.Add("HitText", hitText);
	}

	private static bool IsInHpRange(Creature target)
	{
		int maxHp = target.MaxHp;
		int currentHp = target.CurrentHp;
		return currentHp <= maxHp * 3 / 4 && currentHp >= maxHp / 4;
	}
}
