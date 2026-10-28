using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using Squ.Relics;
using Squ.Script;
using STS2RitsuLib.CardPiles.Nodes;

#nullable enable

namespace Squ.Patches;

/// <summary>Follow the actual reactivation availability without changing button input.</summary>
[HarmonyPatch(typeof(NModCardPileButton), nameof(NModCardPileButton._Process))]
internal static class ScriptCollectionButtonGlowPatch
{
	internal const string GlowNodeName = "ScriptCollectionReactivationGlow";
	private static Shader? _shader;

	[HarmonyPostfix]
	private static void Postfix(NModCardPileButton __instance, Player? ____player, Control ____icon)
	{
		if (__instance.Definition?.PileType != ScriptSystem.ScriptCollectionPile || ____icon is not TextureRect icon)
			return;

		var glow = icon.GetNodeOrNull<TextureRect>(GlowNodeName);
		if (glow is null)
		{
			_shader ??= new Shader { Code = GlowShader };
			glow = new TextureRect
			{
				Name = GlowNodeName,
				Texture = icon.Texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = icon.StretchMode,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ShowBehindParent = true,
				Material = new ShaderMaterial { Shader = _shader },
			};
			icon.AddChild(glow);
			glow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		}

		glow.Visible = ____player?.GetRelic<BoxLunchRelic>()?.CanReselectFromScriptCollection() == true;
		if (glow.Visible)
			((ShaderMaterial)glow.Material).SetShaderParameter("icon_size", icon.Size.Max(Vector2.One));
	}

	// Expand the draw geometry so the halo extends beyond the icon's rectangle.
	// Sample its silhouette and suppress the original fill; the normal icon draws above it.
	private const string GlowShader = """
		shader_type canvas_item;
		render_mode unshaded;
		uniform vec2 icon_size = vec2(80.0);
		const float margin = 12.0;
		float alpha_at(sampler2D tex, vec2 uv) {
			if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) return 0.0;
			return texture(tex, uv).a;
		}
		void vertex() {
			VERTEX += (UV * 2.0 - 1.0) * margin;
		}
		void fragment() {
			vec2 uv = (UV - 0.5) * (icon_size + 2.0 * margin) / icon_size + 0.5;
			float halo = 0.0;
			for (int ring = 1; ring <= 8; ring++) {
				float radius = float(ring) * 1.4;
				float strength = exp(-float(ring * ring) * 0.06);
				for (int i = 0; i < 16; i++) {
					float angle = float(i) * 6.2831853 / 16.0;
					vec2 offset = vec2(cos(angle), sin(angle)) * radius / icon_size;
					halo = max(halo, alpha_at(TEXTURE, uv + offset) * strength);
				}
			}
			COLOR = vec4(0.68, 0.25, 1.0, halo * (1.0 - alpha_at(TEXTURE, uv)) * 0.95);
		}
		""";
}
