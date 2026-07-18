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

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool), StableEntryStem = "the_arrogant_fall")]
public sealed class TheArrogantFall : ModCardTemplate
{
	private const string BuffVarName = "Buff";
	private const string DebuffVarName = "Debuff";
	private const decimal BuffAmount = 7m;

	private static readonly ValueProp BlockProps = ValueProp.Unpowered;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(BuffVarName, BuffAmount),
		new DynamicVar(DebuffVarName, TheArrogantFallPower.DebuffAmountPerStack),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TheArrogantFall.png");

	public TheArrogantFall()
		: base(0, CardType.Skill, CardRarity.Common, CustomTargetType.Anyone)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		decimal buff = DynamicVars[BuffVarName].BaseValue;
		decimal debuff = DynamicVars[DebuffVarName].BaseValue;

		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			cardPlay.Target,
			buff,
			Owner.Creature,
			this);

		await CreatureCmd.GainBlock(cardPlay.Target, buff, BlockProps, cardPlay: null);

		await PowerCmd.Apply<TheArrogantFallPower>(
			choiceContext,
			cardPlay.Target,
			debuff,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}
}
