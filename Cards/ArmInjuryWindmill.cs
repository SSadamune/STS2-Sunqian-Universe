using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "arm_injury_windmill")]
public sealed class ArmInjuryWindmill : ModCardTemplate
{
	private const int BaseDrawCount = 2;
	private const int UpgradedDrawCount = 3;
	private const decimal StatAmount = 2m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseDrawCount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromCard<Wound>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ArmInjuryWindmill.png");

	public ArmInjuryWindmill()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("ArmInjuryWindmill requires an active combat.");

		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

		await PowerCmd.Apply<TempDexFromArmInjuryWindmillPower>(
			choiceContext,
			Owner.Creature,
			StatAmount,
			Owner.Creature,
			this);

		await PowerCmd.Apply<TempStrFromArmInjuryWindmillPower>(
			choiceContext,
			Owner.Creature,
			StatAmount,
			Owner.Creature,
			this);

		await GeneratedCombatCards.AddToDrawPileInCombat<Wound>(
			combatState,
			Owner,
			1,
			upgraded: false,
			Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedDrawCount - BaseDrawCount);
	}
}
