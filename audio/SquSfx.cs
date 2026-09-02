#nullable enable
using STS2RitsuLib.Audio;

namespace Squ.Audio;

/// <summary>
/// 模组短音效：用 <see cref="GameAudioService"/> 播放导入的 Godot 音频资源。
/// </summary>
internal static class SquSfx
{
	public const string WhoseHoundsPath = "res://audio/sfx/WhoseHounds.mp3";
	public const string TraitorDongZhuoPath = "res://audio/sfx/TraitorDongZhuo.mp3";

	public static void Register()
	{
		FmodStudioStreamingFiles.TryPreloadResourceAsSound(WhoseHoundsPath);
		FmodStudioStreamingFiles.TryPreloadResourceAsSound(TraitorDongZhuoPath);
	}

	public static void PlayOneShot(string resourcePath)
	{
		GameAudioService.Shared.PlayOneShot(
			AudioSource.ResourceFile(resourcePath),
			new AudioPlaybackOptions
			{
				Scope = AudioLifecycleScope.Combat,
			});
	}
}
