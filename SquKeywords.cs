using STS2RitsuLib.Content;

#nullable enable

namespace Squ;

/// <summary>
/// Mod 悬停关键词「剧本」的 id 常量（注册见 SquMod.ModLoaded）。
/// </summary>
public static class SquKeywords
{
	public static readonly string ScriptId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "script");
}
