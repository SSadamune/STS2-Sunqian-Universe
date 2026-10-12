using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// A single arrow that falls from above and embeds in a target. Each instance
/// rolls its own trajectory and impact offset so overlapping volleys fan out.
/// </summary>
public partial class NArrowRainVfx : Node2D
{
	public const float ImpactTimeSeconds = 0.26f;

	private const float TravelDistance = 560f;
	private const float MinAngleDegrees = 76f;
	private const float MaxAngleDegrees = 104f;
	private const float ImpactEffectLeadSeconds = 0.1f;
	private const float EmbeddedHoldSeconds = 0.08f;
	private const float FadeSeconds = 0.18f;

	private static readonly Color ImpactTint = new("b1ccca");
	private static readonly Color TrailGlow = new(0.72f, 0.91f, 0.88f, 0.18f);
	private static readonly Color TrailCore = new(0.85f, 0.98f, 0.94f, 0.55f);
	private static readonly Color ShaftOutline = new("211b19");
	private static readonly Color Shaft = new("9a6a3c");
	private static readonly Color ShaftHighlight = new("e4b66f");
	private static readonly Color ArrowheadOutline = new("1b2325");
	private static readonly Color Arrowhead = new("c7d1cf");
	private static readonly Color ArrowheadHighlight = new("f2ffff");
	private static readonly Color FletchingOutline = new("27191a");
	private static readonly Color Fletching = new("a63f42");
	private static readonly Color FletchingHighlight = new("e97865");

	private Vector2 _startPosition;
	private Vector2 _impactPosition;
	private Vector2 _direction;
	private float _elapsed;
	private bool _impactEffectSpawned;
	private bool _hasImpacted;

	public static NArrowRainVfx Create(Vector2 targetCenter)
	{
		float angle = Mathf.DegToRad(Rng.Chaotic.NextFloat(MinAngleDegrees, MaxAngleDegrees));
		Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
		Vector2 impactOffset = new(
			Rng.Chaotic.NextFloat(-24f, 24f),
			Rng.Chaotic.NextFloat(-16f, 14f));
		Vector2 impactPosition = targetCenter + impactOffset;

		return new NArrowRainVfx
		{
			_startPosition = impactPosition - direction * TravelDistance,
			_impactPosition = impactPosition,
			_direction = direction,
			GlobalPosition = impactPosition - direction * TravelDistance,
			Rotation = angle,
			ZIndex = 5,
		};
	}

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;

		if (!_impactEffectSpawned && _elapsed >= ImpactTimeSeconds - ImpactEffectLeadSeconds)
		{
			SpawnImpactEffect();
		}

		if (!_hasImpacted)
		{
			float progress = Mathf.Clamp(_elapsed / ImpactTimeSeconds, 0f, 1f);
			GlobalPosition = _startPosition.Lerp(_impactPosition, progress);
			if (progress >= 1f)
			{
				_hasImpacted = true;
			}
			return;
		}

		float fadeProgress = Mathf.Clamp(
			(_elapsed - ImpactTimeSeconds - EmbeddedHoldSeconds) / FadeSeconds,
			0f,
			1f);
		Modulate = new Color(1f, 1f, 1f, 1f - fadeProgress);
		if (fadeProgress >= 1f)
		{
			this.QueueFreeSafely();
		}
	}

	public override void _Draw()
	{
		// The arrow points along local +X; the whole node is rotated onto its trajectory.
		DrawLine(new Vector2(-62f, 0f), new Vector2(22f, 0f), TrailGlow, 11f, true);
		DrawLine(new Vector2(-50f, 0f), new Vector2(20f, 0f), TrailCore, 3f, true);

		DrawLine(new Vector2(-38f, 0f), new Vector2(19f, 0f), ShaftOutline, 6f, true);
		DrawLine(new Vector2(-37f, 0f), new Vector2(19f, 0f), Shaft, 3.5f, true);
		DrawLine(new Vector2(-35f, -0.8f), new Vector2(18f, -0.8f), ShaftHighlight, 1f, true);

		DrawColoredPolygon(
			[new Vector2(31f, 0f), new Vector2(14f, -10f), new Vector2(18f, 0f), new Vector2(14f, 10f)],
			ArrowheadOutline);
		DrawColoredPolygon(
			[new Vector2(28f, 0f), new Vector2(16f, -7f), new Vector2(19f, 0f), new Vector2(16f, 7f)],
			Arrowhead);
		DrawLine(new Vector2(18f, -5f), new Vector2(27f, -0.5f), ArrowheadHighlight, 1.2f, true);

		DrawColoredPolygon(
			[new Vector2(-38f, 0f), new Vector2(-55f, -10f), new Vector2(-49f, 0f), new Vector2(-55f, 10f)],
			FletchingOutline);
		DrawColoredPolygon(
			[new Vector2(-39f, 0f), new Vector2(-52f, -7f), new Vector2(-47f, 0f), new Vector2(-52f, 7f)],
			Fletching);
		DrawLine(new Vector2(-50f, -5f), new Vector2(-41f, -0.5f), FletchingHighlight, 1f, true);
	}

	private void SpawnImpactEffect()
	{
		_impactEffectSpawned = true;
		NDaggerSprayImpactVfx? impact = NDaggerSprayImpactVfx.Create(
			_impactPosition,
			ImpactTint,
			goingRight: true);
		if (impact == null)
		{
			return;
		}

		impact.Rotation = _direction.Angle();
		GetParent()?.AddChildSafely(impact);
	}
}
