using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 兼职将军专用前摇。场景复制自华丽收场，时间表独立：
/// 0.7 秒压暗并播顶部聚光，停 0.25 秒后在玩家位置播蓄力。
/// 打击牌全部结束后再播收尾粒子，并用 0.5 秒把压暗淡出。
/// </summary>
public partial class NPartTimeGeneralFinaleVfx : Node2D
{
	public const string ScenePath = "res://scenes/vfx/vfx_part_time_general_finale.tscn";

	private const float DarkenSeconds = 0.7f;
	private const float HoldAfterDarkenSeconds = 0.25f;
	private const float FadeOutSeconds = 0.5f;
	private const float EndParticlesHoldSeconds = 2f;

	[Export]
	private Node2D? _spotlight;

	[Export]
	private NParticlesContainer? _spotlightParticles;

	[Export]
	private NParticlesContainer? _anticipationParticles;

	[Export]
	private NParticlesContainer? _slashParticles;

	[Export]
	private NParticlesContainer? _endParticles;

	private Vector2 _playerPosition;
	private Tween? _spotlightTween;
	private CancellationTokenSource? _introCts;
	private bool _outroStarted;

	public static NPartTimeGeneralFinaleVfx? Create(Creature owner)
	{
		if (TestMode.IsOn)
		{
			return null;
		}

		NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		PackedScene? packed = ModSceneCache.Get(ScenePath);
		if (creatureNode == null || container == null || packed == null)
		{
			return null;
		}

		NPartTimeGeneralFinaleVfx? finale = packed.Instantiate<NPartTimeGeneralFinaleVfx>();
		if (finale == null)
		{
			return null;
		}

		finale._playerPosition = creatureNode.VfxSpawnPosition;
		container.AddChildSafely(finale);
		return finale;
	}

	public void PlayOutro()
	{
		if (_outroStarted || !GodotObject.IsInstanceValid(this) || !IsInsideTree())
		{
			return;
		}

		_outroStarted = true;
		_introCts?.Cancel();
		_spotlightTween?.Kill();
		_ = TaskHelper.RunSafely(PlayOutroAsync());
	}

	public override void _Ready()
	{
		if (_spotlight != null)
		{
			_spotlight.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		_slashParticles?.SetEmitting(false);
		_anticipationParticles?.SetEmitting(false);
		_endParticles?.SetEmitting(false);
		_spotlightParticles?.SetEmitting(false);

		if (_spotlightParticles != null)
		{
			Rect2 viewport = GetViewportRect();
			_spotlightParticles.GlobalPosition = new Vector2(viewport.Size.X / 2f, 0f);
		}

		if (_anticipationParticles != null)
		{
			_anticipationParticles.GlobalPosition = _playerPosition;
		}

		if (_endParticles != null)
		{
			_endParticles.GlobalPosition = _playerPosition;
		}

		_ = TaskHelper.RunSafely(PlayIntroAsync());
	}

	public override void _ExitTree()
	{
		_introCts?.Cancel();
		_spotlightTween?.Kill();
	}

	private async Task PlayIntroAsync()
	{
		_introCts = new CancellationTokenSource();
		CancellationToken cancellation = _introCts.Token;
		try
		{
			FadeSpotlight(1f, DarkenSeconds);
			_spotlightParticles?.Restart();
			await Cmd.Wait(DarkenSeconds, cancellation);
			await Cmd.Wait(HoldAfterDarkenSeconds, cancellation);
			if (_outroStarted || cancellation.IsCancellationRequested)
			{
				return;
			}

			_anticipationParticles?.Restart();
		}
		catch (OperationCanceledException)
		{
		}
	}

	private async Task PlayOutroAsync()
	{
		_endParticles?.Restart();
		FadeSpotlight(0f, FadeOutSeconds);
		try
		{
			await Cmd.Wait(EndParticlesHoldSeconds);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
		{
			this.QueueFreeSafely();
		}
	}

	private void FadeSpotlight(float alpha, float seconds)
	{
		if (_spotlight == null || !GodotObject.IsInstanceValid(_spotlight))
		{
			return;
		}

		_spotlightTween?.Kill();
		_spotlightTween = CreateTween();
		_spotlightTween.TweenProperty(
			_spotlight,
			"modulate",
			new Color(1f, 1f, 1f, alpha),
			seconds);
	}
}
