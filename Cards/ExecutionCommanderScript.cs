using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Squ;
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
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(IsUpgraded),
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
		Player player = Owner;
		ICombatState combatState = player.Creature.CombatState
			?? throw new System.InvalidOperationException("ExecutionCommanderScript requires an active combat.");

		await GeneratedCombatCards.AddToHandInCombat<SalvoStrike>(
			combatState,
			player,
			IsUpgraded,
			player);

		await PowerCmd.Apply<ScriptExecutionCommanderPower>(
			choiceContext,
			player.Creature,
			1m,
			player.Creature,
			this);
	}
}
