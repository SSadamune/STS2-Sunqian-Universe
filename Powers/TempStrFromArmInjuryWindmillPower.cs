using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 伤臂大风车施加的临时力量；图标对齐原版 <see cref="SetupStrikePower"/>。
/// </summary>
[RegisterPower]
public sealed class TempStrFromArmInjuryWindmillPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
	private static readonly PowerModel SetupStrikePowerTemplate = ModelDb.Power<SetupStrikePower>();

	public override AbstractModel OriginModel => ModelDb.Card<ArmInjuryWindmill>();

	public PowerAssetProfile AssetProfile => new(
		IconPath: SetupStrikePowerTemplate.PackedIconPath,
		BigIconPath: SetupStrikePowerTemplate.ResolvedBigIconPath);

	public string? CustomIconPath => AssetProfile.IconPath;

	public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
