using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 激光剑行刺：在 <see cref="Hook.ModifyDamage"/> 完整结算（力量、活力等）之后再翻倍。
/// </summary>
[HarmonyPatch]
internal static class LaserSwordAssassinationDamagePatch
{
	private static MethodBase TargetMethod()
	{
		MethodInfo[] methods = AccessTools.GetDeclaredMethods(typeof(Hook))
			.Where(method => method.Name == nameof(Hook.ModifyDamage))
			.ToArray();
		if (methods.Length == 1)
		{
			return methods[0];
		}

		return methods.First(method =>
			method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ModifyDamageHookType)));
	}

	private static void Postfix(
		Creature? target,
		CardModel? cardSource,
		ModifyDamageHookType hookType,
		ref decimal __result)
	{
		if (LaserSwordAssassination.IsUpdatingCardPreview
			|| hookType != ModifyDamageHookType.All
			|| cardSource is not LaserSwordAssassination)
		{
			return;
		}

		if (!LaserSwordAssassination.ShouldDoubleDamage(target))
		{
			return;
		}

		__result *= 2m;
	}
}
