#nullable enable
using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace Squ.Character;

public sealed class SunqianCardPool : TypeListCardPoolModel
{
	public override string Title => "sunqian";
	public override string EnergyColorName => "sunqian";

	public override string? TextEnergyIconPath => "res://images/character/EnergyIcon.png";
	public override string? BigEnergyIconPath => "res://images/character/EnergyIconBig.png";

	public override Color DeckEntryCardColor => new(0.55f, 0.35f, 0.95f);
	public override Color EnergyOutlineColor => new(0.55f, 0.35f, 0.95f);

	private static readonly Material? _poolFrameMaterial =
		MaterialUtils.CreateReplaceHueShaderMaterial(0.55f, 0.35f, 0.95f);

	public override Material? PoolFrameMaterial => _poolFrameMaterial;

	public override bool IsColorless => false;
}
