using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "starlight_sword_saint")]
public sealed class StarlightSwordSaint : ModCardTemplate
{
	public const int DamageAmount = 11;
	public const int UpgradedDamageAmount = 14;
	public const int ScryAmount = 1;
	public const int UpgradedScryAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new ScryVar(ScryAmount),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/StarlightSwordSaint.png");

	public StarlightSwordSaint()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.StarlightSwordSaintPlayEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
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
			SquSfx.Play(SquSfx.StarlightSwordSaintExhaustEvent);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
		DynamicVars[ScryVar.VarName].UpgradeValueBy(UpgradedScryAmount - ScryAmount);
	}
}
