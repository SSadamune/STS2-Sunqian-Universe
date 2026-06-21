using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Character;

public sealed class SunqianPotionPool : TypeListPotionPoolModel
{
	public override string? TextEnergyIconPath => "res://icon.svg";
	public override string? BigEnergyIconPath => "res://icon.svg";

	public override string EnergyColorName => "sunqian";
}
