using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "one_man_one_city")]
public sealed class OneManOneCity : ScriptCardTemplate
{
	public const decimal PlatingStacks = 6m;

	public const decimal UpgradedPlatingStacks = 9m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<PlatingPower>(PlatingStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromPowerWithPowerHoverTips<PlatingPower>(
			(int)DynamicVars[nameof(PlatingPower)].BaseValue),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/OneManOneCity.png");

	public OneManOneCity()
		: base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<PlatingPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(PlatingPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptOneManOneCityPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(PlatingPower)].UpgradeValueBy(UpgradedPlatingStacks - PlatingStacks);
	}
}
