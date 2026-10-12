using System.Collections.Generic;
using System.Linq;
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
	private static readonly Color BlackTint = Colors.Black;
	private const float SweepingBeamImpactDelaySeconds = 0.5f;

	public static void FireBurst(Creature target) =>
		Spawn(NFireBurstVfx.Create(target, 0.75f));

	public static void ThinSlice(Creature target) =>
		Spawn(NThinSliceVfx.Create(target));

	public static void HyperbeamImpact(Creature source, Creature target) =>
		Spawn(NHyperbeamImpactVfx.Create(source, target));

	public static Task SmallMagicMissile(Creature target) =>
		SmallMagicMissiles([target]);

	public static Task SmallMagicMissiles(IEnumerable<Creature> targets) =>
		LaunchSmallMagicMissiles(targets, SmallMissileTint);

	public static Task ArrowRain(Creature target) =>
		ArrowRain([target], layersPerTarget: 1);

	public static Task ArrowRain(IEnumerable<Creature> targets) =>
		ArrowRain(targets, layersPerTarget: 1);

	public static async Task ArrowRain(IEnumerable<Creature> targets, int layersPerTarget)
	{
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (container == null || layersPerTarget <= 0)
		{
			return;
		}

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

			for (int layer = 0; layer < layersPerTarget; layer++)
			{
				container.AddChildSafely(NArrowRainVfx.Create(creatureNode.VfxSpawnPosition));
				any = true;
			}
		}

		if (any)
		{
			await Cmd.Wait(NArrowRainVfx.ImpactTimeSeconds);
		}
	}

	public static async Task BlackSmallMagicMissileAndStarryImpact(Creature target)
	{
		await LaunchSmallMagicMissiles([target], BlackTint);
		VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_starry_impact");
	}

	public static async Task BlackSweepingBeam(Creature owner, IEnumerable<Creature> targets)
	{
		List<Creature> aliveTargets = targets.Where(target => target.IsAlive).ToList();
		if (aliveTargets.Count == 0)
		{
			return;
		}

		NSweepingBeamVfx? beam = NSweepingBeamVfx.Create(owner, aliveTargets);
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (beam == null || container == null)
		{
			return;
		}

		TintParticles(beam, BlackTint);
		container.AddChildSafely(beam);
		await Cmd.Wait(SweepingBeamImpactDelaySeconds);
		TintActiveSweepingBeamImpacts(BlackTint);
	}

	public static NGrandFinaleVfx? StartGrandFinale(Creature owner)
	{
		NGrandFinaleVfx? finale = NGrandFinaleVfx.Create(owner);
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (finale == null || container == null)
		{
			return null;
		}

		container.AddChildSafely(finale);
		return finale;
	}

	public static NGrandFinaleVfx? EnsureGrandFinale(Creature owner, NGrandFinaleVfx? current) =>
		IsGrandFinalePlaying(current) ? current : StartGrandFinale(owner);

	public static void StopGrandFinale(NGrandFinaleVfx? finale)
	{
		if (!IsGrandFinalePlaying(finale))
		{
			return;
		}

		finale!.QueueFreeSafely();
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

	private static async Task LaunchSmallMagicMissiles(IEnumerable<Creature> targets, Color tint)
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
				tint);
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

	private static bool IsGrandFinalePlaying(NGrandFinaleVfx? finale) =>
		finale != null
		&& GodotObject.IsInstanceValid(finale)
		&& !finale.IsQueuedForDeletion();

	private static void TintActiveSweepingBeamImpacts(Color tint)
	{
		Node? root = NCombatRoom.Instance?.GetTree()?.Root;
		if (root == null)
		{
			return;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is NSweepingBeamImpactVfx)
			{
				TintParticles(child, tint);
			}
		}
	}

	private static void TintParticles(Node node, Color tint)
	{
		if (node is GpuParticles2D particles)
		{
			particles.SelfModulate = tint;
		}

		foreach (Node child in node.GetChildren())
		{
			TintParticles(child, tint);
		}
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
