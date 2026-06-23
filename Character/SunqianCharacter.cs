#nullable enable
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace Squ.Character;

[RegisterCharacter]
public sealed class SunqianCharacter : ModCharacterTemplate<SunqianCardPool, SunqianRelicPool, SunqianPotionPool>
{
	public override Color NameColor => new(0.55f, 0.35f, 0.95f);
	public override Color EnergyLabelOutlineColor => new(0.55f, 0.35f, 0.95f);
	public override Color MapDrawingColor => new(0.55f, 0.35f, 0.95f);

	public override CharacterGender Gender => CharacterGender.Masculine;

	public override int StartingHp => 75;
	public override int StartingGold => 99;

	public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
		CharacterAssetProfiles.Ironclad(),
		new(
			Scenes: new(
				VisualsPath: "res://scenes/sunqian_character.tscn",
				EnergyCounterPath: "res://scenes/sunqian_energy_counter.tscn",
				MerchantAnimPath: "res://scenes/sunqian_character_merchant.tscn",
				RestSiteAnimPath: "res://scenes/sunqian_character_rest_site.tscn"
			),
			Ui: new(
				IconTexturePath: "res://icon.svg",
				IconPath: "res://scenes/sunqian_icon.tscn",
				CharacterSelectBgPath: "res://scenes/sunqian_bg.tscn",
				CharacterSelectIconPath: "res://icon.svg",
				CharacterSelectLockedIconPath: "res://icon.svg",
				MapMarkerPath: "res://icon.svg"
			),
			Audio: new(
				CharacterTransitionSfx: "event:/sfx/ui/wipe_ironclad"
			)
		));

	public override float AttackAnimDelay => 0f;
	public override float CastAnimDelay => 0f;

	public override bool RequiresEpochAndTimeline => false;

	protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
		RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

	public override List<string> GetArchitectAttackVfx() =>
	[
		"vfx/vfx_attack_blunt",
		"vfx/vfx_heavy_blunt",
		"vfx/vfx_attack_slash",
		"vfx/vfx_bloody_impact",
		"vfx/vfx_rock_shatter",
	];
}
