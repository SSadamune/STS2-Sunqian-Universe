using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Script;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本能力基类：获得新的剧本能力时，先前的剧本能力会失效。
/// </summary>
public abstract class ScriptPowerTemplate : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<string> RegisteredKeywordIds => [SquKeywords.ScriptId];

	public sealed override async Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		await base.AfterApplied(applier, cardSource);
		await OnScriptApplied(applier, cardSource);
	}

	/// <summary>
	/// Initializes script-specific state before the outgoing script is lifted.
	/// </summary>
	protected virtual Task OnScriptApplied(Creature? applier, CardModel? cardSource) =>
		Task.CompletedTask;

	public override async Task AfterRemoved(Creature oldOwner)
	{
		await base.AfterRemoved(oldOwner);

		if (!ScriptSystem.SuppressLiftNotification)
		{
			await ScriptSystem.NotifyScriptLiftedAsync(oldOwner, new ThrowingPlayerChoiceContext());
		}
	}
}
