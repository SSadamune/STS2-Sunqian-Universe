using System.Collections.Generic;
using System.Linq;
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
	private List<ScriptPowerTemplate>? _scriptsToReplace;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<string> RegisteredKeywordIds => [SquKeywords.ScriptId];

	public sealed override async Task BeforeApplied(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		// Keep the outgoing script alive until this power has been initialized and
		// attached. Its lift effects can draw or auto-play cards, which the incoming
		// script must be able to observe.
		_scriptsToReplace = target.Powers
			.OfType<ScriptPowerTemplate>()
			.Where(ShouldReplaceActiveScript)
			.ToList();
		await base.BeforeApplied(target, amount, applier, cardSource);
	}

	public sealed override async Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		await base.AfterApplied(applier, cardSource);
		await OnScriptApplied(applier, cardSource);

		List<ScriptPowerTemplate> scriptsToReplace = _scriptsToReplace ?? [];
		_scriptsToReplace = null;
		foreach (ScriptPowerTemplate active in scriptsToReplace)
		{
			if (Owner.Powers.Contains(active))
			{
				await ScriptSystem.RemoveScriptPowerAsync(active);
			}
		}
	}

	protected virtual bool ShouldReplaceActiveScript(ScriptPowerTemplate active) => true;

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
