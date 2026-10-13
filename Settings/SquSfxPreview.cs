#nullable enable
using System;
using Squ.Audio;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Data;

namespace Squ.Settings;

/// <summary>
/// 设置页试听：与战斗相同的实例音量 × 游戏音效总线；拧滑条或切换条目时打断上一句。
/// </summary>
internal static class SquSfxPreview
{
	public const string DefaultId = "cybertron";

	private const string Channel = "sunqian_sfx_preview";

	private static IAudioHandle? _handle;

	public static string NormalizeId(string? id) => id switch
	{
		"yiling" or "xiliang" or "cybertron" or "voice" or "eat"
			or "meteor" or "hole" or "poet" => id,
		_ => DefaultId,
	};

	public static string[] EventsFor(string? id) => NormalizeId(id) switch
	{
		"yiling" => [SquSfx.YilingFineFireEvent],
		"xiliang" => [SquSfx.XiliangSavageEvent],
		"cybertron" => [SquSfx.LuXunCybertronEvent],
		"voice" => SquSfx.VoiceChangeEvents,
		"meteor" => SquSfx.FlyingFireMeteorEvents,
		"hole" => SquSfx.TransparentHoleEvents,
		"poet" => SquSfx.TwoWordPoetEvents,
		_ => [SquSfx.ExactlyWhatToEatEvent],
	};

	public static void PlaySelected()
	{
		Stop();

		float volume = SquSettings.SfxLinearMultiplier;
		if (volume <= 0f)
		{
			return;
		}

		string previewId = NormalizeId(
			ModDataStore.For(SquMod.ModId).Get<SquSettings>(SquSettings.DataKey).PreviewSfxId);
		string[] events = EventsFor(previewId);
		if (events.Length == 0)
		{
			return;
		}

		string eventPath = events[Random.Shared.Next(events.Length)];
		AudioPlayResult result = GameFmod.Playback.PlayOneShot(
			AudioSource.Event(eventPath),
			new AudioPlaybackOptions
			{
				Volume = volume,
				UseVanillaRouting = false,
				AllowFadeOutOnStop = false,
				CooldownMs = 0,
				Scope = AudioLifecycleScope.Manual,
				Routing = new AudioRoutingOptions
				{
					Channel = Channel,
					ChannelMode = AudioChannelMode.ReplaceExisting,
					AllowFadeOutOnReplace = false,
				},
			});

		_handle = result.Handle;
	}

	private static void Stop()
	{
		_handle?.TryStop(false);
		_handle?.Dispose();
		_handle = null;
		GameFmod.Playback.StopChannel(Channel, allowFadeOut: false);
	}
}
