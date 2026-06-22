using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ;

/// <summary>
/// Mod 悬停关键词「剧本」的 id 常量（注册见 SquMod.ModLoaded）。
/// </summary>
public static class SquKeywords
{
	public static readonly string ScriptId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "script");

	public static readonly CardKeyword Script = ScriptId.GetModCardKeyword();
}
