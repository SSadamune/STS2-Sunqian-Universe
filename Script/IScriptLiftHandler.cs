using System.Threading.Tasks;

#nullable enable

namespace Squ.Script;

/// <summary>
/// 剧本失效时由 <see cref="ScriptSystem" /> 扫描并分发；实现方自行根据
/// <see cref="ScriptLiftContext.LiftsThisTurn" /> 判断触发条件。
/// </summary>
public interface IScriptLiftHandler
{
	Task OnScriptLiftAsync(ScriptLiftContext context);
}
