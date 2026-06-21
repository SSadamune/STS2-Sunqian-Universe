using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class HujinHuyuanTempDexPower : ModTemporaryPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/HujinHuyuanPower.png",
		BigIconPath: "res://images/powers/HujinHuyuanPower.png");

	public override AbstractModel OriginModel => ModelDb.Power<HujinHuyuanPower>();

	public override PowerModel InternallyAppliedPower => ModelDb.Power<DexterityPower>();

	public override LocString Title => new("powers", "SUNQIAN_UNIVERSE_POWER_HUJIN_HUYUAN_TEMP_DEX_POWER.title");

	public override LocString Description => new("powers", "SUNQIAN_UNIVERSE_POWER_HUJIN_HUYUAN_TEMP_DEX_POWER.description");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];
}
