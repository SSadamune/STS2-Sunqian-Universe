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
	/// <summary>
	/// 若角色身上存在剧本能力，则移除并通知盒饭等遗物（遗物加能效果不受解除影响）。
	/// </summary>
	public static async Task<bool> TryLiftScriptAsync(PlayerChoiceContext choiceContext, Creature creature)
	{
		ScriptPowerTemplate? active = creature.Powers
			.OfType<ScriptPowerTemplate>()
			.FirstOrDefault();

		if (active is null)
		{
			return false;
		}

		await PowerCmd.Remove(active);

		if (creature.Player?.GetRelic<BoxLunchRelic>() is { } boxLunch)
		{
			await boxLunch.OnScriptLiftedAsync(choiceContext);
		}

		return true;
	}
}
