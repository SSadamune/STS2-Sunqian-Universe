using Godot;
using HarmonyLib;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 悬停提示等路径会把已释放的 <see cref="CompressedTexture2D"/> 赋给 <see cref="TextureRect"/>，
/// 触发 ObjectDisposedException。赋值前校验实例有效性，无效则改为 null。
/// </summary>
[HarmonyPatch(typeof(TextureRect))]
internal static class TextureRectDisposedTexturePatch
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(TextureRect.SetTexture))]
	private static bool SetTexturePrefix(TextureRect __instance, Texture2D texture)
	{
		if (texture != null && !GodotObject.IsInstanceValid(texture))
		{
			__instance.Texture = null;
			return false;
		}

		return true;
	}
}
