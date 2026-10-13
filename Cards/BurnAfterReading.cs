using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "burn_after_reading")]
public sealed class BurnAfterReading : ModCardTemplate
{
	public const int TinderStacks = 3;
	public const int UpgradedTinderStacks = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<TinderPower>(TinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(
			(int)DynamicVars[nameof(TinderPower)].BaseValue),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BurnAfterReading.png");

	public BurnAfterReading()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.BurnAfterReadingPlayEvent);
		await PowerCmd.Apply<BurnAfterReadingPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(TinderPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(TinderPower)].UpgradeValueBy(UpgradedTinderStacks - TinderStacks);
	}
}
