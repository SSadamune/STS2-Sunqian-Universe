using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "hate_the_heavens_strike")]
public sealed class HateTheHeavensStrike : ModCardTemplate
{
	public const int DamageAmount = 8;
	public const int UpgradedDamageAmount = 11;
	public const int ScryAmount = 2;
	public const int UpgradedScryAmount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new ScryVar(ScryAmount),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/HateTheHeavensStrike.png");

	public HateTheHeavensStrike()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("HateTheHeavensStrike requires an active combat.");
		SquSfx.Play(SquSfx.HateTheHeavensStrikePlayEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(combatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		ScryResult scryResult = await ScryCmd.Execute(
			choiceContext,
			Owner,
			DynamicVars.Scry().IntValue,
			static (context, card) => CardCmd.Exhaust(context, card),
			source: TitleLocString);

		if (scryResult.Discarded.Count > 0)
		{
			SquSfx.PlayRandom(RunState, SquSfx.HateTheHeavensStrikeExhaustEvents);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
		DynamicVars[ScryVar.VarName].UpgradeValueBy(UpgradedScryAmount - ScryAmount);
	}
}
