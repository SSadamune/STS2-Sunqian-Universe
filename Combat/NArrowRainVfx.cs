using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// A silver projectile streak that falls from above. Each instance rolls its
/// own trajectory and impact offset so overlapping volleys fan out.
/// </summary>
public partial class NArrowRainVfx : Node2D
{
	public const float ImpactTimeSeconds = 0.26f;

	private const float TravelDistance = 560f;
	private const float MinAngleDegrees = 76f;
	private const float MaxAngleDegrees = 104f;
	private const float ImpactEffectLeadSeconds = 0.1f;

	private static readonly Color ImpactTint = new("d2d2d2");
	private static readonly Color TrailShadow = new(0f, 0f, 0f, 0.72f);
	private static readonly Color TrailSilver = new("c7cbd0");

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
				QueueRedraw();
				this.QueueFreeSafely();
			}
		}
	}

	public override void _Draw()
	{
		if (_hasImpacted)
		{
			return;
		}

		// One silver projectile streak over a slightly offset black shadow.
		DrawLine(
			new Vector2(-96f, 2.5f),
			new Vector2(18f, 2.5f),
			TrailShadow,
			7f,
			true);
		DrawLine(
			new Vector2(-96f, 0f),
			new Vector2(18f, 0f),
			TrailSilver,
			3.5f,
			true);
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
