#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace Squ.Monsters;

/// <summary>Placeholder guardian. Its combat behavior comes from DieForYouPower.</summary>
[RegisterMonster]
public sealed class SecondBrother : ModMonsterTemplate
{
	private const string Portrait = "res://images/powers/GuanYuMartialVerPowerBig.png";
	public override int MinInitialHp => 1;
	public override int MaxInitialHp => 1;
	public override bool HasDeathSfx => false;
	public override bool ShouldShowInCompendium => false;
	public override bool IsHealthBarVisible => Creature.IsAlive;
	public override MonsterAssetProfile AssetProfile => new(
		VisualsScenePath: "res://scenes/second_brother.tscn");
	protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
		RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
	protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
		Node visualsRoot, MonsterModel monster) =>
		ModAnimStateMachines.StandardCue(visualsRoot, null, idleName: "idle", deadName: "dead",
			hitName: "hit", attackName: "attack", castName: "cast",
			cueSet: ModVisualCues.CueSet().Single("idle", Portrait)
				.Single("dead", Portrait).Single("hit", Portrait, 0.15f)
				.Single("attack", Portrait, 0.15f).Single("cast", Portrait, 0.15f).Build());

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState idle = new("DO_NOTHING", static _ => Task.CompletedTask);
		idle.FollowUpState = idle;
		return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
	}
}
