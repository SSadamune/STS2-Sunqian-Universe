using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

#nullable enable

namespace Squ.Potions;

/// <summary>
/// 将沛国佳酿的商店实付价格固定为 100 金币，不受药水基础价与商店随机浮动影响。
/// </summary>
[RegisterSingleton]
public sealed class PeiguoBrewMerchantPrice : HookedSingletonModel
{
	public PeiguoBrewMerchantPrice()
		: base(HookType.Run)
	{
	}

	public override decimal ModifyMerchantPrice(
		Player player,
		MerchantEntry entry,
		decimal cost) =>
		entry is MerchantPotionEntry { Model: PeiguoBrewPotion }
			? PeiguoBrewPotion.MerchantPrice
			: cost;
}
