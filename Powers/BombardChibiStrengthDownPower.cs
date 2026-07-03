using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 炮轰赤壁剧本失效时施加的临时减力量（参考 <see cref="EnfeeblingTouchPower"/>）。
/// </summary>
[RegisterPower]
public sealed class BombardChibiStrengthDownPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
	protected override bool IsPositive => false;

	public override AbstractModel OriginModel => ModelDb.Card<BombardChibiScript>();

	public PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BombardChibiStrengthDownPower.png",
		BigIconPath: "res://images/powers/BombardChibiStrengthDownPowerBig.png");

	public string? CustomIconPath => AssetProfile.IconPath;

	public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
