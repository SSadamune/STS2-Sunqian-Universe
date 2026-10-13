using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Monsters;

/// <summary>变异半人马召唤的坐骑；视觉场景、名称和生命值均由绑定敌人配置。</summary>
[RegisterMonster]
public sealed class MutatedCentaurMount : ModMonsterTemplate
{
	private string _boundEnemyName = string.Empty;
	private string _visualsPath = SceneHelper.GetScenePath("creature_visuals/fallback");
	private int _initialHp = 1;

	public override LocString Title
	{
		get
		{
			LocString title = base.Title;
			title.Add("EnemyName", _boundEnemyName);
			return title;
		}
	}

	public override int MinInitialHp => _initialHp;

	public override int MaxInitialHp => _initialHp;

	public override bool HasDeathSfx => false;

	public override bool ShouldShowInCompendium => false;

	protected override string VisualsPath => _visualsPath;

	internal void Configure(string boundEnemyName, string visualsPath, int initialHp)
	{
		_boundEnemyName = boundEnemyName;
		_visualsPath = visualsPath;
		_initialHp = initialHp;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState idle = new("DO_NOTHING", static _ => Task.CompletedTask);
		idle.FollowUpState = idle;
		return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
	}
}
