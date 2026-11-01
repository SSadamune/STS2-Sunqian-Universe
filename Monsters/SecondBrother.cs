#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Monsters;

[RegisterMonster]
public sealed class SecondBrother : ModMonsterTemplate
{
    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;
    public override string DeathSfx => "event:/sfx/characters/osty/osty_die";
    public override bool ShouldShowInCompendium => false;
    public override bool IsHealthBarVisible => Creature.IsAlive;
    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/osty");
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        ModelDb.Monster<Osty>().GenerateAnimator(controller);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("NOTHING_MOVE", static _ => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }
}
