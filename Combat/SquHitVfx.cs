using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Spawns vanilla hit effects on the combat VFX layer, at the moment the calling attack reaches that beat.
/// </summary>
internal static class SquHitVfx
{
	private static readonly Color FireMissileTint = new("FF8B57");
	private static readonly Color SmallMissileTint = new("50b598");

	public static void FireBurst(Creature target) =>
		Spawn(NFireBurstVfx.Create(target, 0.75f));

	public static void ThinSlice(Creature target) =>
		Spawn(NThinSliceVfx.Create(target));

	public static void HyperbeamImpact(Creature source, Creature target) =>
		Spawn(NHyperbeamImpactVfx.Create(source, target));

	public static Task SmallMagicMissile(Creature target) =>
		SmallMagicMissiles([target]);

	public static async Task SmallMagicMissiles(IEnumerable<Creature> targets)
	{
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (container == null)
		{
			return;
		}

		float wait = 0f;
		bool any = false;
		foreach (Creature target in targets)
		{
			if (!target.IsAlive)
			{
				continue;
			}

			NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
			if (creatureNode == null)
			{
				continue;
			}

			NSmallMagicMissileVfx? missile = NSmallMagicMissileVfx.Create(
				creatureNode.GetBottomOfHitbox(),
				SmallMissileTint);
			if (missile == null)
			{
				continue;
			}

			container.AddChildSafely(missile);
			wait = missile.WaitTime;
			any = true;
		}

		if (any)
		{
			await Cmd.Wait(wait);
		}
	}

	public static async Task LargeMagicMissile(Creature target)
	{
		NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
		if (creatureNode == null)
		{
			return;
		}

		NLargeMagicMissileVfx? missile = NLargeMagicMissileVfx.Create(
			creatureNode.GetBottomOfHitbox(),
			FireMissileTint);
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (missile == null || container == null)
		{
			return;
		}

		container.AddChildSafely(missile);
		await Cmd.Wait(missile.WaitTime);
	}

	private static void Spawn(Node2D? vfx)
	{
		if (vfx == null)
		{
			return;
		}

		NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
	}
}
