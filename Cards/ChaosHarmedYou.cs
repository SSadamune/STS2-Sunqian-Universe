using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "chaos_harmed_you")]
public sealed class ChaosHarmedYou : ModCardTemplate
{
	public const decimal BaseDamage = 22m;
	public const decimal UpgradedDamage = 33m;
	private const int BaseDrawOnKill = 2;
	private const int UpgradedDrawOnKill = 3;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unpowered;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, DamageProps),
		new CardsVar(BaseDrawOnKill),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(Squ.SquKeywords.Environmental),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ChaosHarmedYou.png");

	public ChaosHarmedYou()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		if (attackCommand.Results.SelectMany(results => results).Any(result => result.WasTargetKilled))
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars.Cards.UpgradeValueBy(UpgradedDrawOnKill - BaseDrawOnKill);
	}
}
