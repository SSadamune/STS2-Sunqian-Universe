using System.Collections.Generic;
using Squ;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 与剧本机制相关的遗物基类：自动附带「剧本」关键词悬停提示。
/// </summary>
public abstract class ScriptRelicTemplate : ModRelicTemplate
{
	protected override IEnumerable<string> RegisteredKeywordIds => [SquKeywords.ScriptId];
}
