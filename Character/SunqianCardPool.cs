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

	// Grape purple theme for card frames and deck UI.
	private static readonly Color ThemeColor = new("6B3FA0");
	private static readonly Color ThemeOutlineColor = new("3F2568");

	public override Color DeckEntryCardColor => ThemeColor;
	public override Color EnergyOutlineColor => ThemeOutlineColor;

	private static readonly Material? _poolFrameMaterial =
		MaterialUtils.CreateReplaceHueShaderMaterial(
			ThemeColor.R,
			ThemeColor.G,
			ThemeColor.B);

	public override Material? PoolFrameMaterial => _poolFrameMaterial;

	public override bool IsColorless => false;
}
