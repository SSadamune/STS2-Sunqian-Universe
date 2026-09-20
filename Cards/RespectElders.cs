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
using Squ.Audio;
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
	private const string CanMultiHitVarName = "CanMultiHit";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(7m, ValueProp.Move),
		new DynamicVar("HitCount", BaseHitCount),
		ModCardVars.Computed(CanMultiHitVarName, 0,
			(CardModel? card, Creature? target) =>
				target != null && IsInHpRange(target) ? 1 : 0),
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

		bool extraHits = IsInHpRange(cardPlay.Target);
		SquSfx.Play(extraHits
			? SquSfx.RespectEldersSpareTheYoungAndOldEvent
			: SquSfx.RespectEldersTooOldEvent);

		int hitCount = extraHits
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
		if (DynamicVars[CanMultiHitVarName].PreviewValue > 0)
		{
			var hitConfirm = new LocString("cards", Id.Entry + ".hitConfirm");
			hitConfirm.Add(DynamicVars.Damage);
			hitConfirm.Add(DynamicVars["HitCount"]);
			description.Add("BodyText", hitConfirm);
			return;
		}

		var bodyText = new LocString("cards", Id.Entry + ".normalBody");
		bodyText.Add(DynamicVars.Damage);
		bodyText.Add(DynamicVars["HitCount"]);
		description.Add("BodyText", bodyText);
	}

	private static bool IsInHpRange(Creature target)
	{
		int maxHp = target.MaxHp;
		int currentHp = target.CurrentHp;
		// Compare via cross-multiply so 1/4 and 3/4 are exact (46/4 must not become 11).
		return currentHp * 4 >= maxHp && currentHp * 4 <= maxHp * 3;
	}
}
