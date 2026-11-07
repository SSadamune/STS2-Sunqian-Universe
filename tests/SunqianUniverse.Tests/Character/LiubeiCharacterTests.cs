using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;
using Squ.Cards;
using Squ.Character;
using Squ.Relics;
using STS2RitsuLib.Scaffolding.Characters;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Character;

public sealed class LiubeiCharacterTests : SunqianCombatTestSuite
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shared_actor_cards_are_registered_once_in_both_pools_with_original_mode_constraints(bool multiplayer)
    {
        Type[] shared =
        [
            typeof(WatchFireFromShore), typeof(WorkLifeBalance), typeof(ThunderOnStillLake),
            typeof(KeepPlayingKeepDancing), typeof(BlockFirefighting), typeof(RuthlessStrike),
            typeof(SelfDecapitationAscension), typeof(BurningLinkedCampsScript), typeof(MilkDragonEmerges),
        ];
        var constraint = multiplayer ? CardMultiplayerConstraint.MultiplayerOnly : CardMultiplayerConstraint.SingleplayerOnly;
        foreach (var pool in new CardPoolModel[] { ModelDb.CardPool<SunqianCardPool>(), ModelDb.CardPool<LiubeiCardPool>() })
        {
            var cards = pool.GetUnlockedCards(Player.UnlockState, constraint).ToArray();
            foreach (Type type in shared)
            {
                int expected = type == typeof(SelfDecapitationAscension) && !multiplayer ? 0 : 1;
                Assert.Equal(expected, cards.Count(card => card.GetType() == type));
            }
        }
    }

    [Theory]
    [InlineData(typeof(LureAndRelease))]
    [InlineData(typeof(RevengeStrike))]
    [InlineData(typeof(WhenFloatOneCup))]
    [InlineData(typeof(CannotGetDrunk))]
    [InlineData(typeof(DeflectStrike))]
    [InlineData(typeof(HeroesSympathy))]
    public void New_cards_belong_only_to_the_reincarnation_prisoner_pool(Type cardType)
    {
        ModelId id = ModelDb.GetId(cardType);
        Assert.Contains(
            ModelDb.CardPool<LiubeiCardPool>().AllCards,
            card => card.Id == id);
        Assert.DoesNotContain(
            ModelDb.CardPool<SunqianCardPool>().AllCards,
            card => card.Id == id);
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("liubei-character-registration");

    [Fact]
    public void Liubei_is_a_hidden_variant_with_its_own_pool_and_starting_loadout()
    {
        var policy = Assert.IsAssignableFrom<IModCharacterVanillaSelectionPolicy>(Player.Character);

        Assert.True(policy.HideFromVanillaCharacterSelect);
        Assert.False(policy.AllowInVanillaRandomCharacterSelect);
        Assert.False(policy.HideInCardLibraryCompendium);
        Assert.IsType<LiubeiCardPool>(Player.Character.CardPool);
        Assert.Equal(4, Player.Deck.Cards.Count(card => card is StrikeLiubei));
        Assert.Equal(4, Player.Deck.Cards.Count(card => card is DefendLiubei));
        Assert.Single(Player.Deck.Cards.OfType<StandByRighteousness>());
        Assert.Single(Player.Deck.Cards.OfType<SoulContract>());
        Assert.Equal(10, Player.Deck.Cards.Count);
        Assert.IsType<DeathDeniedRelic>(Assert.Single(Player.Character.StartingRelics));
        Assert.Contains(
            "res://images/scenes/LiubeiCharacterSelectIcon.png",
            Player.Character.AssetPathsCharacterSelect);
        Assert.NotNull(Player.Character.CharacterSelectIcon);
        Assert.All(
            Player.Character.AssetPaths.Concat(Player.Character.AssetPathsCharacterSelect),
            path => Assert.DoesNotContain("ReincarnationPrisoner", path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Liubei_shares_character_progress_with_Sunqian()
    {
        CharacterModel sunqian = ModelDb.Character<SunqianCharacter>();
        var progress = new ProgressState();

        Assert.Same(
            progress.GetOrCreateCharacterStats(sunqian.Id),
            progress.GetOrCreateCharacterStats(Player.Character.Id));
    }

    [Fact]
    public void Liubei_variant_replaces_the_shared_character_select_tab_icon()
    {
        using var button = new NCharacterSelectButton();
        using var icon = new TextureRect();
        Traverse.Create(button).Field<TextureRect>("_icon").Value = icon;

        Type patchType = AccessTools.TypeByName("Squ.Patches.ActorVariantCharacterSelectPatch");
        AccessTools.Method(patchType, "RefreshAnchorIcon").Invoke(null, [button, Player.Character]);

        Assert.Same(Player.Character.CharacterSelectIcon, icon.Texture);
    }

    [Fact]
    public void Actor_variant_selector_reuses_the_native_gold_arrow_visuals()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(
            "res://scenes/screens/character_select_screen.tscn");
        using NCharacterSelectScreen screen = scene.Instantiate<NCharacterSelectScreen>();
        NButton source = screen.GetNode<NButton>(
            "AscensionPanel/HBoxContainer/LeftArrowContainer/LeftArrow");
        source.Visible = false;

        Type patchType = AccessTools.TypeByName("Squ.Patches.ActorVariantCharacterSelectPatch");
        using NButton clone = Assert.IsAssignableFrom<NButton>(
            AccessTools.Method(patchType, "CloneNativeArrow")
                .Invoke(null, [source, "TestActorVariantArrow"]));
        TextureRect sourceIcon = source.GetNode<TextureRect>("TextureRect");
        TextureRect cloneIcon = clone.GetNode<TextureRect>("TextureRect");

        Assert.IsType<NGoldArrowButton>(clone);
        Assert.True(clone.Visible);
        Assert.Same(sourceIcon.Texture, cloneIcon.Texture);
        Assert.NotSame(sourceIcon.Material, cloneIcon.Material);

        AccessTools.Method(patchType, "PositionArrow").Invoke(null, [clone, true]);
        Assert.Equal(0f, clone.AnchorLeft);
        Assert.Equal(0.5f, clone.AnchorTop);
        Assert.True(clone.OffsetRight < 0f);
        Assert.Equal(-147f, (clone.OffsetLeft + clone.OffsetRight) / 2f);
        Assert.Equal(117f, clone.OffsetBottom - clone.OffsetTop);

        AccessTools.Method(patchType, "PositionArrow").Invoke(null, [clone, false]);
        Assert.Equal(1f, clone.AnchorLeft);
        Assert.True(clone.OffsetLeft > 0f);
        Assert.Equal(147f, (clone.OffsetLeft + clone.OffsetRight) / 2f);
    }
}
