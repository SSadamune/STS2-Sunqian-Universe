using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "keep_playing_keep_dancing")]
public sealed class KeepPlayingKeepDancing : ModCardTemplate
{
	public const decimal BaseVigor = 3m;
	public const decimal BaseKeepVigor = 2m;
	public const decimal UpgradedKeepVigor = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
		new PowerVar<KeepVigorPower>(BaseKeepVigor),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		..HoverTipFactory.FromPowerWithPowerHoverTips<KeepVigorPower>(
			(int)DynamicVars[nameof(KeepVigorPower)].BaseValue),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KeepEnjoying.png");

	public KeepPlayingKeepDancing()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<KeepVigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(KeepVigorPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(KeepVigorPower)].UpgradeValueBy(UpgradedKeepVigor - BaseKeepVigor);
	}
}
