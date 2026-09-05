using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "execution_commander_script")]
public sealed class ExecutionCommanderScript : ScriptCardTemplate
{
	public const int GeneratedStrikeCount = 2;
	public const decimal BaseBonusPercent = 50m;
	public const decimal UpgradedBonusPercent = 100m;

	private const string BonusPercentVarName = "BonusPercent";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(BonusPercentVarName, BaseBonusPercent),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(IsUpgraded),
		HoverTipFactory.FromKeyword(SquKeywords.StackableScript),
		HoverTipFactory.FromKeyword(SquKeywords.MultiTarget),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ExecutionCommanderScript.png");

	public ExecutionCommanderScript()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.ExecutionerArchersReadyEvent);
		Player player = Owner;
		ICombatState combatState = player.Creature.CombatState
			?? throw new System.InvalidOperationException("ExecutionCommanderScript requires an active combat.");

		for (int i = 0; i < GeneratedStrikeCount; i++)
		{
			await GeneratedCombatCards.AddToHandInCombat<SalvoStrike>(
				combatState,
				player,
				IsUpgraded,
				player);
		}

		await PowerCmd.Apply<ScriptExecutionCommanderPower>(
			choiceContext,
			player.Creature,
			DynamicVars[BonusPercentVarName].BaseValue,
			player.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[BonusPercentVarName].UpgradeValueBy(UpgradedBonusPercent - BaseBonusPercent);
	}
}
