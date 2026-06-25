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
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class Jiaobingbibai : ModCardTemplate
{
	private const decimal VigorAmount = 7m;
	private const decimal BlockAmount = 7m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(VigorAmount),
		new BlockVar(BlockAmount, ValueProp.Move),
		new PowerVar<WeakPower>(JiaobingPower.DebuffAmountPerStack),
		new PowerVar<VulnerablePower>(JiaobingPower.DebuffAmountPerStack),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Jiaobingbibai.png");

	public Jiaobingbibai()
		: base(0, CardType.Power, CardRarity.Common, CustomTargetType.Anyone)
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

		await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay: null);

		await PowerCmd.Apply<JiaobingPower>(
			choiceContext,
			cardPlay.Target,
			JiaobingPower.DebuffAmountPerStack,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}
}
