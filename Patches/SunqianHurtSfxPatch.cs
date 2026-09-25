#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;

namespace Squ.Patches;

/// <summary>
/// 孙乾在战斗中被敌方攻击实际打掉生命时播放受击语音。
/// 自伤、生命流失、事件伤害与完全格挡均不触发。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
internal static class SunqianHurtSfxPatch
{
	[HarmonyPrefix]
	private static void PlayHurtSfx(
		ICombatState? combatState,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer)
	{
		if (combatState is null
			|| target.Player?.Character is not SunqianCharacter
			|| result.UnblockedDamage <= 0
			|| !props.IsPoweredAttack()
			|| dealer is not { IsEnemy: true }
			|| !LocalContext.IsMe(target))
		{
			return;
		}

		SquSfx.Play(SquSfx.SunqianHurtEvent);
	}
}
