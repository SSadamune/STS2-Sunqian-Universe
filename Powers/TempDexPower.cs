using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower(Inherit = true)]
public abstract class TempDexPower<T> : ModTemporaryAppliedPowerTemplate<T, DexterityPower>
	where T : AbstractModel
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TempDexPower.png",
		BigIconPath: "res://images/powers/TempDexPowerBig.png");

	public override LocString Description => new("powers", "SUNQIAN_UNIVERSE_POWER_TEMP_DEX_POWER.description");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];
}

[RegisterPower]
public sealed class TempDexFromPhasingPower : TempDexPower<PhasingPower> { }

[RegisterPower]
public sealed class TempDexFromSunqianScriptPower : TempDexPower<SunqianScript> { }

[RegisterPower]
public sealed class TempDexFromArmInjuryWindmillPower : TempDexPower<ArmInjuryWindmill> { }
