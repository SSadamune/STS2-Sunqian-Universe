using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Powers;
using Squ.Relics;

#nullable enable

namespace Squ.Script;

public static class ScriptSystem
{
	internal static bool SuppressLiftNotification { get; set; }

	/// <summary>
	/// 移除角色身上所有剧本能力（例如事件强制结束剧本）。
	/// </summary>
	public static async Task InvalidateScriptsAsync(Creature creature)
	{
		foreach (ScriptPowerTemplate active in creature.Powers.OfType<ScriptPowerTemplate>().ToList())
		{
			await RemoveScriptPowerAsync(active);
		}
	}

	public static async Task RemoveScriptPowerAsync(ScriptPowerTemplate power, bool notifyLift = true)
	{
		SuppressLiftNotification = !notifyLift;
		await PowerCmd.Remove(power);
		SuppressLiftNotification = false;
	}

	internal static async Task NotifyScriptLiftedAsync(Creature creature, PlayerChoiceContext choiceContext)
	{
		if (creature.Player?.GetRelic<BoxLunchRelic>() is { } boxLunch)
		{
			await boxLunch.OnScriptLiftedAsync(choiceContext);
		}

		if (creature.GetPower<SunqianUniversePower>() is { } sunqianUniversePower)
		{
			await sunqianUniversePower.OnScriptLiftedAsync(choiceContext);
		}
	}
}
