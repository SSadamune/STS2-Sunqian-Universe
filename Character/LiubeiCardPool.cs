#nullable enable
using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace Squ.Character;

public sealed class LiubeiCardPool : TypeListCardPoolModel
{
	public override string Title => "liubei";
	public override string EnergyColorName => "liubei";

	public override string? TextEnergyIconPath => "res://images/character/EnergyIcon.png";
	public override string? BigEnergyIconPath => "res://images/character/EnergyIconBig.png";

	private static readonly Color ThemeColor = new("4D7780");
	private static readonly Color ThemeOutlineColor = new("263F48");

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
