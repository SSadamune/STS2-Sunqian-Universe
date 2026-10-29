using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Squ.Cards;
using Squ.Patches;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class ScriptInteractionTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-interactions");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exclusive_form_replaces_its_partner_before_the_older_regular_script(bool civilFirst)
    {
        await Play(await AddToHand<GuanDiForm>());
        CardModel older = Combat.CreateCard<ServantScriptRecord>(Player);
        CardModel civil = Combat.CreateCard<GuanYuCivilVer>(Player);
        CardModel martial = Combat.CreateCard<GuanYuMartialVer>(Player);
        CardModel first = civilFirst ? civil : martial;
        CardModel second = civilFirst ? martial : civil;
        var selected = new HashSet<CardModel>();
        var order = new ScriptActivationSelectionOrder();
        selected.Add(older); order.RecordResult(selected, older);
        selected.Add(first); order.RecordResult(selected, first);
        Assert.Same(first, order.MakeRoomForScript(selected, second, 2));
        selected.Remove(first); selected.Add(second); order.RecordResult(selected, second);
        Assert.Equal(new[] { older, second }, order.Confirm(selected));
    }

    [Fact]
    public void Confirm_preserves_click_order_after_deselection_reselection_and_hash_slot_reuse()
    {
        CardModel a = ModelDb.Card<ServantScriptRecord>();
        CardModel b = ModelDb.Card<SunqianScriptRecord>();
        CardModel c = ModelDb.Card<HeadsmanScriptRecord>();
        var selected = new HashSet<CardModel>();
        var order = new ScriptActivationSelectionOrder();
        Click(a); Click(b); Click(a); Click(c); Click(a);
        // Deliberately feed an unrelated enumeration order at confirmation.
        Assert.Equal(new[] { b, c, a }, order.Confirm(new[] { a, b, c }));
        void Click(CardModel card)
        {
            if (!selected.Remove(card)) selected.Add(card);
            order.RecordResult(selected, card);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Screen_confirmation_returns_click_order_instead_of_hash_set_order(bool simple)
    {
        MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardGridSelectionScreen screen = simple
            ? new MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NSimpleCardSelectScreen()
            : new MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCombatPileCardSelectScreen();
        try
        {
            var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(
                new MegaCrit.Sts2.Core.Localization.LocString(SquCommonL10n.Table, ScriptSystem.SelectionPromptKey), 2);
            CardModel a = ModelDb.Card<ServantScriptRecord>();
            CardModel b = ModelDb.Card<SunqianScriptRecord>();
            var selected = new HashSet<CardModel> { b };
            CardGridSelectionRotation.AfterClick(screen, b, prefs, selected);
            selected.Add(a);
            CardGridSelectionRotation.AfterClick(screen, a, prefs, selected);
            var task = screen.CardsSelected();
            var source = (TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(
                typeof(MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardGridSelectionScreen), "_completionSource").GetValue(screen)!;
            source.SetResult(new[] { a, b });
            Assert.Equal(new[] { b, a }, await task);
        }
        finally { screen.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Activation_effects_mounting_and_bulk_lifts_follow_selection_order(bool reverse)
    {
        await PlayerCmd.SetEnergy(20, Player);
        await ApplyPower<GuanDiFormPower>(Player.Creature, 1);
        var sunqian = await AddToHand<SunqianScript>();
        var headsman = await AddToHand<HeadsmanScript>();
        await ScriptSystem.UnlockOrReinforceAsync<SunqianScriptRecord>(new BlockingPlayerChoiceContext(), sunqian, 1, activate: false);
        await ScriptSystem.UnlockOrReinforceAsync<HeadsmanScriptRecord>(new BlockingPlayerChoiceContext(), headsman, 1, activate: false);
        var records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        var first = records.OfType<SunqianScriptRecord>().Single();
        var second = records.OfType<HeadsmanScriptRecord>().Single();
        CardModel[] selection = reverse ? [second, first] : [first, second];
        Type[] expected = reverse ? [typeof(ScriptHeadsmanPower), typeof(ScriptSunqianPower)]
            : [typeof(ScriptSunqianPower), typeof(ScriptHeadsmanPower)];
        var applied = new List<Type>();
        var removed = new List<Type>();
        void OnApplied(PowerModel power) => applied.Add(power.GetType());
        void OnRemoved(PowerModel power) { if (power is ScriptPowerTemplate) removed.Add(power.GetType()); }
        Player.Creature.PowerApplied += OnApplied;
        Player.Creature.PowerRemoved += OnRemoved;
        try
        {
            var selector = new ScriptedCardSelector().ThenSelect(selection);
            using (UseCardSelector(selector))
                Assert.True(await ScriptSystem.ChooseActiveScriptsAsync(new BlockingPlayerChoiceContext(), Player, false));
            AssertAllSelectionsConsumed(selector);
            Assert.Equal(expected, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Select(power => power.GetType()));
            Assert.Equal(expected, applied.Where(type => typeof(ScriptPowerTemplate).IsAssignableFrom(type)));
            Assert.Equal(reverse, applied.IndexOf(typeof(VigorPower)) < applied.IndexOf(typeof(TempDexPower)));
            await ScriptSystem.InvalidateScriptsAsync(Player.Creature);
            Assert.Equal(expected, removed);
        }
        finally
        {
            Player.Creature.PowerApplied -= OnApplied;
            Player.Creature.PowerRemoved -= OnRemoved;
        }
    }

    [Fact]
    public async Task Record_frame_is_applied_to_previews_and_restored_for_normal_cards()
    {
        var record = Combat.CreateCard<GuanYuMartialVer>(Player);
        var node = PreloadManager.Cache.GetScene("res://scenes/cards/card.tscn").Instantiate<NCard>();
        NGame.Instance!.AddChild(node);
        try
        {
            var title = node.GetNode<Control>("%TitleLabel");
            var originalTitle = new Rect2(title.Position, title.Size);
            var portrait = node.GetNode<Control>("%Portrait");
            var originalPortrait = new Rect2(portrait.Position, portrait.Size);
            var plaque = node.GetNode<Control>("%TypePlaque");
            var originalPlaqueScale = plaque.Scale;
            node.Model = record;
            node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            var frame = node.GetNode<TextureRect>("%Frame");
            Assert.Equal(ScriptRecordCardFrame.TexturePath, frame.Texture.ResourcePath);
            Assert.Null(frame.Material);
            Assert.Equal(originalPlaqueScale, plaque.Scale);
            float dividerCenterY = frame.Position.Y + frame.Size.Y * ScriptRecordCardFrame.PortraitDividerFraction;
            Assert.Equal(dividerCenterY, plaque.Position.Y + plaque.Size.Y * 0.5f, 3);
            Assert.Equal(130, node.GetNode<Control>("%DescriptionLabel").Size.Y);
            Assert.Equal(ScriptRecordCardFrame.TitleBandCenterY, title.Position.Y + title.Size.Y * 0.5f);
            Assert.True(title.Position.Y + title.Size.Y < portrait.Position.Y, $"Title y={title.Position.Y}, height={title.Size.Y}; portrait y={portrait.Position.Y}");
            node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            Assert.Equal(dividerCenterY, plaque.Position.Y + plaque.Size.Y * 0.5f, 3);
            Assert.Equal(ScriptRecordCardFrame.TitleBandCenterY, title.Position.Y + title.Size.Y * 0.5f);
            Assert.False(node.GetNode<TextureRect>("%TitleBanner").Visible);
            Assert.False(node.GetNode<TextureRect>("%PortraitBorder").Visible);
            node.Model = await AddToHand<SunqianScript>();
            Assert.NotEqual(ScriptRecordCardFrame.TexturePath, frame.Texture.ResourcePath);
            Assert.Equal(originalPlaqueScale, plaque.Scale);
            Assert.Equal(originalTitle, new Rect2(title.Position, title.Size));
            Assert.Equal(originalPortrait, new Rect2(portrait.Position, portrait.Size));
            Assert.True(node.GetNode<TextureRect>("%TitleBanner").Visible);
            Assert.True(node.GetNode<TextureRect>("%PortraitBorder").Visible);
        }
        finally { node.QueueFree(); }
        string? previewPath = System.Environment.GetEnvironmentVariable("SUNQIAN_SCRIPT_FRAME_PREVIEW");
        if (!string.IsNullOrEmpty(previewPath) && DisplayServer.GetName() != "headless")
            await CaptureFramePreview(previewPath);
    }

    private async Task CaptureFramePreview(string path)
    {
        var layer = new CanvasLayer { Layer = 100 };
        NGame.Instance!.AddChild(layer);
        try
        {
            var background = new ColorRect { Color = new Color("182329"), Size = new Vector2(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
            layer.AddChild(background);
            var form = await AddToHand<GuanDiForm>();
            var grunt = await AddToHand<GruntScript>();
            var bowang = await AddToHand<BowangParadoxScript>();
            var neutral = await AddToHand<NeutralAmbushScript>();
            CardModel[] models = [neutral, neutral.CreateScriptRecordPreviews().First(),
                form.CreateScriptRecordPreviews().First()];
            for (int index = 0; index < models.Length; index++)
            {
                var preview = PreloadManager.Cache.GetScene("res://scenes/cards/card.tscn").Instantiate<NCard>();
                layer.AddChild(preview);
                preview.Model = models[index];
                preview.Position = new Vector2(350 + index * 600, 530);
                preview.Scale = new Vector2(1.5f, 1.5f);
                preview.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            }
            await NGame.Instance.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await NGame.Instance.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            NGame.Instance.GetViewport().GetTexture().GetImage().SavePng(path);
        }
        finally { layer.QueueFree(); }
    }
}

