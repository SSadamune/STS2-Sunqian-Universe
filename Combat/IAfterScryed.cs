using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 由模型实现，在一次[gold]预见[/gold]完全结算后（已查看、已选择、弃牌已进入弃牌堆）触发。
/// 仅当实际发生了预见时才会触发：修改后的数量大于 0 且抽牌堆至少有一张牌。
/// </summary>
public interface IAfterScryed
{
	/// <param name="scryAmount">经修改后请求的预见数量，未按抽牌堆实际张数裁剪，可能大于被查看的张数。</param>
	/// <param name="discardAmount">玩家选择弃掉的张数，始终等于 <paramref name="discarded"/> 的数量。</param>
	/// <param name="discarded">本次预见弃掉的牌，运行到此时已被加入弃牌堆；玩家全部保留时为空。</param>
	Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, IEnumerable<CardModel> discarded);
}
