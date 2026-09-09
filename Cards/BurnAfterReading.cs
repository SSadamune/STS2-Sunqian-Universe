using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "burn_after_reading")]
public sealed class BurnAfterReading : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(BurnAfterReadingPower.EnergyGain),
		new PowerVar<TinderPower>(BurnAfterReadingPower.TinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.ForEnergy(this),
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(
			(int)DynamicVars[nameof(TinderPower)].BaseValue),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BurnAfterReading.png");

	public BurnAfterReading()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<BurnAfterReadingPower>(
			choiceContext,
			Owner.Creature,
			IsUpgraded ? BurnAfterReadingPower.UpgradedTriggerCount : BurnAfterReadingPower.BaseTriggerCount,
			Owner.Creature,
			this);
	}
}
