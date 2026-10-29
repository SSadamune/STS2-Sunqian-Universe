using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanDiFormPower : ModPowerTemplate, IScriptActivationLimitProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuCivilVerPower.png",
        BigIconPath: "res://images/powers/GuanYuCivilVerPowerBig.png");
    public int GetAdditionalScriptActivationSlots(Player player, int unlockedScriptCount) => Amount;
}
