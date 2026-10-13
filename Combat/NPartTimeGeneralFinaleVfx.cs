using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 兼职将军专用前摇。画面节点来自华丽收场场景，时间表独立：
/// 0.7 秒压暗并播顶部聚光，停 0.25 秒后在玩家位置播蓄力。
/// 打击牌全部结束后再播收尾粒子，并用 0.5 秒把压暗淡出。
/// </summary>
public partial class NPartTimeGeneralFinaleVfx : Node2D
{
	private const float DarkenSeconds = 0.7f;
	private const float HoldAfterDarkenSeconds = 0.25f;
	private const float FadeOutSeconds = 0.5f;
	private const float EndParticlesHoldSeconds = 2f;

	private const BindingFlags FieldFlags =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private Node2D? _spotlight;
	private NParticlesContainer? _spotlightParticles;
	private NParticlesContainer? _anticipationParticles;
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
		if (creatureNode == null || container == null)
		{
			return null;
		}

		NGrandFinaleVfx source = PreloadManager.Cache
			.GetScene(NGrandFinaleVfx.scenePath)
			.Instantiate<NGrandFinaleVfx>();
		var finale = new NPartTimeGeneralFinaleVfx();
		if (!finale.TryAdopt(source, creatureNode.VfxSpawnPosition))
		{
			source.QueueFreeSafely();
			finale.QueueFreeSafely();
			return null;
		}

		source.QueueFreeSafely();
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

	private bool TryAdopt(NGrandFinaleVfx source, Vector2 playerPosition)
	{
		_spotlight = ReadField<Node2D>(source, "_spotlight");
		_spotlightParticles = ReadField<NParticlesContainer>(source, "_spotlightParticles");
		_anticipationParticles = ReadField<NParticlesContainer>(source, "_anticipationParticles");
		_endParticles = ReadField<NParticlesContainer>(source, "_endParticles");
		NParticlesContainer? slashParticles =
			ReadField<NParticlesContainer>(source, "_slashParticles");
		if (_spotlight == null
			|| _spotlightParticles == null
			|| _anticipationParticles == null
			|| _endParticles == null)
		{
			return false;
		}

		_playerPosition = playerPosition;
		Godot.Collections.Array<Node> children = source.GetChildren();
		foreach (Node child in children)
		{
			source.RemoveChild(child);
			AddChild(child);
		}

		_spotlight.Modulate = new Color(1f, 1f, 1f, 0f);
		_spotlightParticles.SetEmitting(false);
		_anticipationParticles.SetEmitting(false);
		_endParticles.SetEmitting(false);
		slashParticles?.SetEmitting(false);
		return true;
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

	private static T? ReadField<T>(object source, string fieldName)
		where T : class
	{
		return typeof(NGrandFinaleVfx)
			.GetField(fieldName, FieldFlags)
			?.GetValue(source) as T;
	}
}
