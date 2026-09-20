using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using Squ.Character;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 角色选择界面显示孙乾背景（bg169）时播放《关羽之歌》，切走或返回主菜单时恢复原 BGM。
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen))]
internal static class SunqianCharacterSelectBgmPatch
{
	private static bool _embarking;

	[HarmonyPrefix]
	[HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuOpened))]
	private static void OnSubmenuOpenedPrefix()
	{
		_embarking = false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
	private static void SelectCharacterPostfix(
		NCharacterSelectButton charSelectButton,
		CharacterModel characterModel)
	{
		if (!charSelectButton.IsLocked && characterModel is SunqianCharacter)
		{
			SunqianSelectBgm.Play();
			return;
		}

		SunqianSelectBgm.RestoreMenuIfPlaying();
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnLocalCharacterChangedForRandom")]
	private static void OnLocalCharacterChangedForRandomPostfix(CharacterModel characterModel)
	{
		if (characterModel is SunqianCharacter)
		{
			SunqianSelectBgm.Play();
			return;
		}

		SunqianSelectBgm.RestoreMenuIfPlaying();
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(NCharacterSelectScreen.BeginRun))]
	private static void BeginRunPrefix()
	{
		_embarking = true;
		SunqianSelectBgm.StopWithoutRestore();
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuClosed))]
	private static void OnSubmenuClosedPostfix()
	{
		if (_embarking)
		{
			SunqianSelectBgm.StopWithoutRestore();
			return;
		}

		SunqianSelectBgm.RestoreMenuIfPlaying();
	}
}
