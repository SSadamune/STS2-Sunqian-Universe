using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "yiling_battle_script")]
public sealed class YilingBattleScript : ScriptCardTemplate
{
	public const decimal FuelAbundantStacks = 3m;

	public const decimal UpgradedFuelAbundantStacks = 5m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FuelAbundantPower>(FuelAbundantStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromPowerWithPowerHoverTips<FuelAbundantPower>(
			(int)DynamicVars[nameof(FuelAbundantPower)].BaseValue),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromCard<ShangfangguSigh>(upgrade: false),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/YilingBattleScript.png");

	public YilingBattleScript()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal fuelAbundantStacks = DynamicVars[nameof(FuelAbundantPower)].BaseValue;
		await PowerCmd.Apply<FuelAbundantPower>(
			choiceContext,
			Owner.Creature,
			fuelAbundantStacks,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptYilingBattlePower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(FuelAbundantPower)].UpgradeValueBy(UpgradedFuelAbundantStacks - FuelAbundantStacks);
	}
}
