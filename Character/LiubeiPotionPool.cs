#nullable enable
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Character;

public sealed class LiubeiPotionPool : TypeListPotionPoolModel
{
	public override string? TextEnergyIconPath => "res://images/character/EnergyIcon.png";
	public override string? BigEnergyIconPath => "res://images/character/EnergyIconBig.png";

	public override string EnergyColorName => "liubei";
}
