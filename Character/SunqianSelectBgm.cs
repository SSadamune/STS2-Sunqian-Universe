#nullable enable
using Godot;
using STS2RitsuLib.Audio;

namespace Squ.Character;

/// <summary>
/// 角色选择界面选中孙乾（显示 bg169 背景）时播放的 BGM。
/// 音乐由模组 FMOD Bank 提供，并路由到游戏的 music 总线。
/// </summary>
internal static class SunqianSelectBgm
{
	public const string EventPath = "event:/sunqian_universe/music/SongOfGuanYu";

	private const string MenuMusicEventPath = "event:/music/menu_update";

	private static GodotObject? _music;

	public static void Play()
	{
		if (_music != null)
		{
			return;
		}

		// 孙乾选人曲接管菜单音乐时，先停止原版唯一音乐槽，避免叠音。
		GameFmod.Studio.StopMusic();

		GodotObject? instance = FmodStudioEventInstances.TryCreate(EventPath);
		if (instance == null)
		{
			SquMod.Logger.Error($"[Audio] Failed to create Sunqian select BGM event: {EventPath}");
			PlayVanillaMenuMusic();
			return;
		}

		if (!FmodStudioEventInstances.TryStart(instance))
		{
			FmodStudioEventInstances.TryRelease(instance);
			SquMod.Logger.Error($"[Audio] Failed to start Sunqian select BGM event: {EventPath}");
			PlayVanillaMenuMusic();
			return;
		}

		_music = instance;
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
		GodotObject? instance = _music;
		_music = null;
		if (instance == null)
		{
			return;
		}

		FmodStudioEventInstances.TryStop(instance, allowFadeOut: false);
		FmodStudioEventInstances.TryRelease(instance);
	}

	private static void PlayVanillaMenuMusic()
	{
		// 原版菜单负责这条 BGM；必须走原版唯一音乐槽，不能创建并遗失新的托管句柄。
		GameFmod.Studio.PlayMusic(MenuMusicEventPath);
	}
}
