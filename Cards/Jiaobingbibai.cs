using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class Jiaobingbibai : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(7),
		new PowerVar<VulnerablePower>(3),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Jiaobingbibai.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
	];

	public Jiaobingbibai()
		: base(1, CardType.Skill, CardRarity.Uncommon, CustomTargetType.Anyone)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<VulnerablePower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(VulnerablePower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
