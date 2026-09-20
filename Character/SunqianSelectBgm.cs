#nullable enable
using STS2RitsuLib.Audio;

namespace Squ.Character;

/// <summary>
/// 角色选择界面选中孙乾（显示 bg169 背景）时播放的 BGM。
/// </summary>
internal static class SunqianSelectBgm
{
	public const string ResourcePath = "res://audio/music/SongOfGuanYu.mp3";

	private const string MenuMusicEventPath = "event:/music/menu_update";
	private const string Channel = "sunqian_select_bgm";

	private static AudioMusicHandle? _music;

	public static void Register()
	{
		FmodStudioStreamingFiles.TryPreloadResourceAsStreamingMusic(ResourcePath);
	}

	public static void Play()
	{
		if (_music is { IsValid: true })
		{
			return;
		}

		StopOurMusic();

		// 流式 MP3 不会替换原版 Studio 菜单曲，先让出原版音乐槽，避免叠音。
		GameFmod.Studio.StopMusic();

		AudioMusicHandle? handle = GameAudioService.Shared.PlayMusic(
			AudioSource.StreamingResourceMusic(ResourcePath),
			new AudioPlaybackOptions
			{
				Scope = AudioLifecycleScope.Manual,
				Routing = new AudioRoutingOptions
				{
					Channel = Channel,
					ChannelMode = AudioChannelMode.ReplaceExisting,
				},
			});

		if (handle is not { IsValid: true })
		{
			handle?.Dispose();
			_music = null;
			PlayVanillaMenuMusic();
			return;
		}

		_music = handle;
	}

	public static void RestoreMenuIfPlaying()
	{
		if (_music == null)
		{
			return;
		}

		StopOurMusic();
		PlayVanillaMenuMusic();
	}

	public static void StopWithoutRestore()
	{
		StopOurMusic();
	}

	private static void StopOurMusic()
	{
		_music?.TryStop();
		_music?.Dispose();
		_music = null;
	}

	private static void PlayVanillaMenuMusic()
	{
		// 原版菜单负责这条 BGM；必须走原版唯一音乐槽，不能创建并遗失新的托管句柄。
		GameFmod.Studio.PlayMusic(MenuMusicEventPath);
	}
}
