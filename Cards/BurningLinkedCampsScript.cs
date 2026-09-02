using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "burning_linked_camps_script")]
public sealed class BurningLinkedCampsScript : ScriptCardTemplate
{
	public const decimal TinderStacks = 4m;

	public const decimal UpgradedTinderStacks = 8m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<TinderPower>(TinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(
			(int)DynamicVars[nameof(TinderPower)].BaseValue),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromCard<ShangfangguSigh>(upgrade: false),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BurningLinkedCampsScript.png");

	public BurningLinkedCampsScript()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.EmperorKnowsNoWarEvent);
		decimal tinderStacks = DynamicVars[nameof(TinderPower)].BaseValue;
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			Owner.Creature,
			tinderStacks,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptBurningLinkedCampsPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(TinderPower)].UpgradeValueBy(UpgradedTinderStacks - TinderStacks);
	}
}
