using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// 新三国公开 API 的可选依赖代理。不要引用 newsanguo.dll。
/// 目标模组未加载时 <see cref="IsReady"/> 为 false，其余成员不会被调用。
/// stub 必须是单一 ret 的简单返回；不要 throw。RitsuLib 用 InsertBeforeSingleRet 改写，
/// throw 会导致该成员失败并中止整个类型的互操作绑定。
/// </summary>
[ModInterop("newsanguo", "newsanguo.Scripts.Api.NewsanguoPublicApi")]
public static class NewsanguoPublicApiInterop
{
	public static bool IsReady => false;

	public static bool IsNewsanguoCharacter(CharacterModel? character) => false;

	public static Task ApplyDrunkenMight(
		PlayerChoiceContext choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false) => Task.CompletedTask;

	public static IHoverTip CreateDrunkenMightHoverTip() => null!;
}
