using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>All script records share a scroll-bound folio frame, including hover previews.</summary>
internal static class ScriptRecordCardFrame
{
    internal const string TexturePath = "res://images/ui/script_record_frame.png";
    internal const float TitleBandCenterY = -159f;
    internal const float TitleLabelHeight = 36f;
    internal const float PortraitDividerFraction = 808f / 1490f;
    private sealed class Layout
    {
        public bool Applied;
        public Rect2 DescriptionRect;
        public Rect2 TitleRect;
        public Rect2 PortraitRect;
        public Vector2 PlaquePosition;
        public Vector2 PlaqueScale;
        public Vector2 PlaquePivot;
        public Texture2D? ShadowTexture;
    }
    private static readonly ConditionalWeakTable<NCard, Layout> Layouts = new();
    private static Texture2D? _texture;

    internal static void Apply(NCard card)
    {
        var frame = card.GetNode<TextureRect>("%Frame");
        var banner = card.GetNode<TextureRect>("%TitleBanner");
        var border = card.GetNode<TextureRect>("%PortraitBorder");
        var description = card.GetNode<Control>("%DescriptionLabel");
        var shadow = card.GetNode<TextureRect>("CardContainer/Shadow");
        var title = card.GetNode<Control>("%TitleLabel");
        var portrait = card.GetNode<Control>("%Portrait");
        var plaque = card.GetNode<Control>("%TypePlaque");
        var layout = Layouts.GetValue(card, _ => new Layout());
        if (card.Model is not ScriptRecordCardTemplate)
        {
            // NCard nodes can be reused; vanilla restores the texture/material.
            if (layout.Applied)
            {
                banner.Visible = card.Model?.Rarity != CardRarity.Ancient;
                border.Visible = card.Model?.Rarity != CardRarity.Ancient;
                description.Position = layout.DescriptionRect.Position;
                description.Size = layout.DescriptionRect.Size;
                shadow.Texture = layout.ShadowTexture;
                title.Position = layout.TitleRect.Position;
                title.Size = layout.TitleRect.Size;
                portrait.Position = layout.PortraitRect.Position;
                portrait.Size = layout.PortraitRect.Size;
                plaque.Position = new Vector2(plaque.Position.X, layout.PlaquePosition.Y);
                plaque.Scale = layout.PlaqueScale;
                plaque.PivotOffset = layout.PlaquePivot;
                layout.Applied = false;
            }
            return;
        }
        if (!layout.Applied)
        {
            layout.DescriptionRect = new Rect2(description.Position, description.Size);
            layout.ShadowTexture = shadow.Texture;
            layout.TitleRect = new Rect2(title.Position, title.Size);
            layout.PortraitRect = new Rect2(portrait.Position, portrait.Size);
            layout.PlaquePosition = plaque.Position;
            layout.PlaqueScale = plaque.Scale;
            layout.PlaquePivot = plaque.PivotOffset;
            layout.Applied = true;
        }
        _texture ??= ResourceLoader.Load<Texture2D>(TexturePath);
        frame.Texture = _texture;
        frame.Material = null;
        shadow.Texture = _texture;
        // Use moderate title padding and give the freed space back to the art.
        title.Position = new Vector2(layout.TitleRect.Position.X, TitleBandCenterY - TitleLabelHeight * 0.5f);
        title.Size = new Vector2(layout.TitleRect.Size.X, TitleLabelHeight);
        portrait.Position = new Vector2(layout.PortraitRect.Position.X, -138);
        float dividerCenterY = frame.Position.Y + frame.Size.Y * PortraitDividerFraction;
        plaque.Position = new Vector2(plaque.Position.X, dividerCenterY - plaque.Size.Y * 0.5f);
        plaque.PivotOffset = plaque.Size * 0.5f;
        plaque.Scale = layout.PlaqueScale;
        // Keep long rules inside the narrower bound manuscript panel.
        description.Position = new Vector2(-112, 37);
        description.Size = new Vector2(224, 130);
        banner.Visible = false;
        border.Visible = false;
    }
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class ScriptRecordCardPortraitFramePatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance) => ScriptRecordCardFrame.Apply(__instance);
}

// Reload assigns the pool's frame shader after UpdatePortrait; clear it again so
// the artwork's teal leather and brass are not recolored like a normal skill.
[HarmonyPatch(typeof(NCard), "Reload")]
internal static class ScriptRecordCardReloadFramePatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        if (__instance.IsNodeReady() && __instance.Model is not null)
            ScriptRecordCardFrame.Apply(__instance);
    }
}


// Vanilla resizes this plaque after its translated label is laid out.
[HarmonyPatch(typeof(NCard), "UpdateTypePlaqueSizeAndPosition")]
internal static class ScriptRecordCardPlaqueLayoutPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        if (__instance.Model is ScriptRecordCardTemplate)
            ScriptRecordCardFrame.Apply(__instance);
    }
}
