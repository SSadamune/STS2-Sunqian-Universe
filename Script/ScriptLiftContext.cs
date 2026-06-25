using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

#nullable enable

namespace Squ.Script;

/// <summary>
/// 单次剧本失效时分发给 <see cref="IScriptLiftHandler" /> 的上下文。
/// </summary>
public readonly struct ScriptLiftContext
{
	public required PlayerChoiceContext ChoiceContext { get; init; }

	public required Creature Owner { get; init; }

	/// <summary>
	/// 本回合第几次剧本失效（从 1 起计，含本次）。
	/// </summary>
	public required int LiftsThisTurn { get; init; }

	public bool IsFirstLiftOfTurn => LiftsThisTurn == 1;
}
