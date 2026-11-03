using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Squ.Patches;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Monsters;
using STS2RitsuLib;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CompanionSummonTests : SunqianCombatTestSuite
{
    private static bool _testPetsRegistered;
    private static int _modifyCalls;
    private static int _afterCalls;
    private static decimal _modifier;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("companion-summons");

    [Fact]
    public async Task Second_brother_uses_native_osty_visuals_and_animator()
    {
        var result = await OstyCmd.Summon(ChoiceContext(), Player, 6, null);
        using var visuals = result.Creature!.Monster!.CreateVisuals();
        Assert.EndsWith("/osty.tscn", visuals.SceneFilePath);
        visuals._Ready();
        Assert.True(visuals.HasSpineAnimation);
        Assert.NotNull(visuals.SpineBody);
        var animator = result.Creature.Monster.GenerateAnimator(visuals.SpineBody);
        Assert.True(animator.HasTrigger("Attack"));
        Assert.True(animator.HasTrigger("Dead"));
        Assert.True(animator.HasTrigger("Revive"));
    }

    [Fact]
    public async Task Native_creature_widget_displays_second_brother_health_and_both_power_icons()
    {
        var pet = (await OstyCmd.Summon(ChoiceContext(), Player, 6, null)).Creature!;
        var node = PreloadManager.Cache.GetScene("res://scenes/combat/creature.tscn").Instantiate<NCreature>();
        Traverse.Create(node).Property(nameof(NCreature.Entity)).SetValue(pet);
        Traverse.Create(node).Property(nameof(NCreature.Visuals)).SetValue(pet.Monster!.CreateVisuals());
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(node);
            Assert.True(node.IsNodeReady());
            var display = Traverse.Create(node).Field<NCreatureStateDisplay>("_stateDisplay").Value;
            Assert.True(display.Visible);
            Assert.True(node.IsInteractable);
            var powers = Traverse.Create(display).Field<NPowerContainer>("_powerContainer").Value;
            Assert.Contains(powers.GetChildren().OfType<NPower>(), power => power.Model is DieForYouPower);
            Assert.Contains(powers.GetChildren().OfType<NPower>(), power => power.Model is Squ.Powers.LifeContractPower);
            Assert.NotNull(Traverse.Create(display).Field<NHealthBar>("_healthBar").Value);
            Assert.Same(pet.Monster, SecondBrotherPresentationPatches.AsOstyPresentation(pet.Monster));
            Assert.Contains(Harmony.GetPatchInfo(AccessTools.Method(typeof(NCombatRoom), "AddCreature")).Transpilers,
                patch => patch.PatchMethod.DeclaringType == typeof(SecondBrotherPresentationPatches));
        }
        finally
        {
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }
    }

    [Fact]
    public async Task Companion_power_descriptions_name_second_brother_without_changing_osty()
    {
        var pet = (await OstyCmd.Summon(ChoiceContext(), Player, 6, null)).Creature!;
        var guard = pet.GetPower<DieForYouPower>()!;
        Assert.Equal("SUNQIAN_UNIVERSE_POWER_SECOND_BROTHER_DIE_FOR_YOU.description", guard.Description.LocEntryKey);
        Assert.Equal(guard.Description.LocEntryKey, guard.SmartDescription.LocEntryKey);
        Assert.Contains("[gold]", guard.SmartDescription.GetFormattedText());
        var contract = pet.GetPower<Squ.Powers.LifeContractPower>()!;
        var contractText = contract.SmartDescription;
        contract.DynamicVars.AddTo(contractText);
        Assert.Contains("至多回复0点", contractText.GetFormattedText());
        var osty = (await CompanionSummons.SummonSpecific(ChoiceContext(), Player, CompanionSummons.OstyId, 6, null)).Creature!;
        Assert.NotEqual(guard.Description.LocEntryKey, osty.GetPower<DieForYouPower>()!.Description.LocEntryKey);
    }

    [Fact]
    public async Task Native_entry_uses_character_default_and_returns_the_real_pet()
    {
        var result = await OstyCmd.Summon(ChoiceContext(), Player, 6, null);
        Assert.IsType<SecondBrother>(result.Creature!.Monster);
        Assert.Same(Player, result.Creature.PetOwner);
        Assert.Equal(6, result.Creature.CurrentHp);
        Assert.Equal(6, result.Creature.MaxHp);
        Assert.Equal(6m, result.Amount);
        Assert.True(result.Creature.HasPower<DieForYouPower>());
        Assert.Null(Player.Osty);
    }

    [Fact]
    public async Task Native_card_is_redirected_and_hover_names_the_bound_pet()
    {
        var card = await AddToHand<Bodyguard>();
        var tip = Assert.Single(card.HoverTips.OfType<HoverTip>()
            .Where(tip => tip.Title?.Contains("5") == true));
        Assert.Contains(CompanionSummons.GetName(CompanionSummons.SecondBrotherId).GetFormattedText(), tip.Title);
        await Play(card);
        Assert.IsType<SecondBrother>(Assert.Single(Player.Creature.Pets).Monster);
        Assert.Equal(5, Assert.Single(Player.Creature.Pets).CurrentHp);
    }

    [Fact]
    public async Task Other_characters_keep_native_osty_and_can_explicitly_summon_second_brother()
    {
        var other = Players.Single(player => player != Player);
        var ordinary = await OstyCmd.Summon(ChoiceContext(), other, 4, null);
        Assert.IsType<Osty>(ordinary.Creature!.Monster);
        var specific = await CompanionSummons.SummonSpecific(ChoiceContext(), other,
            CompanionSummons.SecondBrotherId, 7, null);
        Assert.IsType<SecondBrother>(specific.Creature!.Monster);
        await OstyCmd.Summon(ChoiceContext(), other, 2, null);
        Assert.Equal(6, ordinary.Creature.MaxHp);
        Assert.Equal(7, specific.Creature.MaxHp);
        Assert.Equal(CompanionSummons.OstyId, CompanionSummons.GetDefaultId(other));
    }

    [Fact]
    public async Task Existing_pet_gains_max_hp_and_heals_by_summon_amount_without_duplicate()
    {
        Creature pet = (await OstyCmd.Summon(ChoiceContext(), Player, 8, null)).Creature!;
        await CreatureCmd.Damage(ChoiceContext(), pet, 3,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        var again = await OstyCmd.Summon(ChoiceContext(), Player, 4, null);
        Assert.Same(pet, again.Creature);
        Assert.Equal(12, pet.MaxHp);
        Assert.Equal(9, pet.CurrentHp);
        Assert.Single(Player.Creature.Pets);
    }

    [Fact]
    public async Task Dead_pet_is_reused_and_revived_at_new_summon_amount()
    {
        Creature pet = (await OstyCmd.Summon(ChoiceContext(), Player, 5, null)).Creature!;
        await CreatureCmd.Kill(pet, force: true);
        Assert.True(pet.IsDead);
        var revived = await OstyCmd.Summon(ChoiceContext(), Player, 3, null);
        Assert.Same(pet, revived.Creature);
        Assert.Equal(3, pet.MaxHp);
        Assert.Equal(3, pet.CurrentHp);
        Assert.Single(Player.Creature.Pets);
    }

    [Fact]
    public async Task Second_brother_absorbs_powered_unblocked_damage_and_overflow_reaches_owner()
    {
        int hp = Player.Creature.CurrentHp;
        var result = await OstyCmd.Summon(ChoiceContext(), Player, 5, null);
        await CreatureCmd.GainBlock(Player.Creature, 2, ValueProp.Unpowered, null);
        await CreatureCmd.Damage(ChoiceContext(), Player.Creature, 10, ValueProp.Move, EnemyAt(0));
        Assert.True(result.Creature!.IsDead);
        Assert.Equal(hp - 3, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Explicit_osty_bypasses_redirect_once_without_changing_default()
    {
        var osty = await CompanionSummons.SummonSpecific(ChoiceContext(), Player,
            CompanionSummons.OstyId, 4, null);
        Assert.IsType<Osty>(osty.Creature!.Monster);
        var next = await OstyCmd.Summon(ChoiceContext(), Player, 6, null);
        Assert.IsType<SecondBrother>(next.Creature!.Monster);
        Assert.Equal(4, osty.Creature.MaxHp);
        Assert.Equal(2, Player.Creature.Pets.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Any_selection_summons_chosen_pet_without_changing_default(bool chooseOsty)
    {
        string id = chooseOsty ? CompanionSummons.OstyId : CompanionSummons.SecondBrotherId;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
            [options.OfType<CompanionSummonChoice>().Single(card => card.CompanionId == id)]);
        SummonResult result;
        using (UseCardSelector(selector))
            result = await CompanionSummons.SummonAny(ChoiceContext(), Player, 7, null);
        AssertAllSelectionsConsumed(selector);
        Assert.Same(result.Creature, CompanionSummons.GetCompanion(Player, id));
        Assert.Equal(7, result.Creature!.MaxHp);
        Assert.Equal(CompanionSummons.SecondBrotherId, CompanionSummons.GetDefaultId(Player));
    }

    [Fact]
    public async Task Any_menu_contains_deduplicated_defaults_and_only_owners_explicit_discoveries()
    {
        RegisterTestPets();
        var other = Players.Single(player => player != Player);
        Assert.Equal([CompanionSummons.OstyId, CompanionSummons.SecondBrotherId],
            CompanionSummons.GetAvailableIds(Player));
        Assert.Equal([CompanionSummons.OstyId], CompanionSummons.GetAvailableIds(other));
        await CompanionSummons.SummonSpecific(ChoiceContext(), Player, "test_mount", 3, null);
        await CompanionSummons.SummonSpecific(ChoiceContext(), Player, "test_mount", 3, null);
        Assert.Contains("test_mount", CompanionSummons.GetAvailableIds(Player));
        Assert.DoesNotContain("test_mount", CompanionSummons.GetAvailableIds(other));
        Assert.Equal(3, CompanionSummons.GetAvailableIds(Player).Count);
        CompanionSummons.ClearCombat(Combat);
        Assert.DoesNotContain("test_mount", CompanionSummons.GetAvailableIds(Player));
    }

    [Fact]
    public async Task Any_grid_supports_more_than_three_candidates_and_reuses_selected_pet()
    {
        RegisterTestPets();
        await CompanionSummons.SummonSpecific(ChoiceContext(), Player, "test_mount", 3, null);
        var dummy = await CompanionSummons.SummonSpecific(ChoiceContext(), Player, "test_dummy", 4, null);
        Assert.Equal(4, CompanionSummons.GetAvailableIds(Player).Count);
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Equal(4, options.Count());
            return [options.OfType<CompanionSummonChoice>().Single(card => card.CompanionId == "test_dummy")];
        });
        using (UseCardSelector(selector))
        {
            var result = await CompanionSummons.SummonAny(ChoiceContext(), Player, 2, null);
            Assert.Same(dummy.Creature, result.Creature);
            Assert.Equal(6, result.Creature!.MaxHp);
        }
        AssertAllSelectionsConsumed(selector);
    }

    [Theory]
    [InlineData(3, 7)]
    [InlineData(-4, 0)]
    public async Task Summon_modifier_runs_once_and_zero_suppresses_success_hooks(int modifier, int expected)
    {
        var harmony = new Harmony("squ.tests.companion-modifier");
        _modifyCalls = _afterCalls = 0;
        _modifier = modifier;
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifySummonAmount)),
            prefix: new HarmonyMethod(typeof(CompanionSummonTests), nameof(ModifyAmount)));
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.AfterSummon)),
            prefix: new HarmonyMethod(typeof(CompanionSummonTests), nameof(CountAfter)));
        try
        {
            var result = await CompanionSummons.SummonSpecific(ChoiceContext(), Player,
                CompanionSummons.SecondBrotherId, 4, null);
            Assert.Equal((decimal)expected, result.Amount);
            Assert.Equal(1, _modifyCalls);
            Assert.Equal(expected > 0 ? 1 : 0, _afterCalls);
            if (expected == 0) Assert.Null(result.Creature);
            else Assert.Equal((int)expected, result.Creature!.MaxHp);
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    [Fact]
    public async Task Explicit_zero_does_not_unlock_an_any_candidate()
    {
        RegisterTestPets();
        await CompanionSummons.SummonSpecific(ChoiceContext(), Player, "test_mount", 0, null);
        Assert.DoesNotContain("test_mount", CompanionSummons.GetAvailableIds(Player));
        Assert.Null(CompanionSummons.GetCompanion(Player, "test_mount"));
    }

    [Fact]
    public async Task Any_zero_base_amount_still_receives_native_summon_modifiers_once()
    {
        var harmony = new Harmony("squ.tests.companion-any-zero");
        _modifier = 3;
        _modifyCalls = 0;
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifySummonAmount)),
            prefix: new HarmonyMethod(typeof(CompanionSummonTests), nameof(ModifyAmount)));
        var selector = new ScriptedCardSelector().ThenSelect(options =>
            [options.OfType<CompanionSummonChoice>().Single(card =>
                card.CompanionId == CompanionSummons.SecondBrotherId)]);
        try
        {
            using (UseCardSelector(selector))
            {
                var result = await CompanionSummons.SummonAny(ChoiceContext(), Player, 0, null);
                Assert.Equal(3m, result.Amount);
                Assert.Equal(3, result.Creature!.MaxHp);
                Assert.Equal(1, _modifyCalls);
            }
            AssertAllSelectionsConsumed(selector);
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    [Fact]
    public void Named_and_any_tooltips_have_dynamic_names_amounts_and_upgraded_values()
    {
        SummonVar amount = new(6);
        var first = (HoverTip)CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, amount);
        Assert.Contains(CompanionSummons.GetName(CompanionSummons.SecondBrotherId).GetFormattedText(), first.Title);
        Assert.Contains("[" + CompanionSummons.GetName(CompanionSummons.SecondBrotherId).GetFormattedText() + "]", first.Title);
        Assert.DoesNotContain("[lb]", first.Title);
        Assert.EndsWith("6", first.Title);
        amount.UpgradeValueBy(2);
        var after = (HoverTip)CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, amount);
        Assert.EndsWith("8", after.Title);
        Assert.Contains("8", after.Description);
        Assert.EndsWith("8", ((HoverTip)CompanionSummonKeywords.AnyHoverTip(amount)).Title);
        Assert.Contains("8", CompanionSummonKeywords.AnyCardText(amount).GetFormattedText());
    }

    private static void RegisterTestPets()
    {
        if (_testPetsRegistered) return;
        _testPetsRegistered = true;
        CompanionSummons.Register<MutatedCentaurMount>("test_mount", "res://images/cards/MutatedCentaur.png");
        CompanionSummons.Register<BigDummy>("test_dummy", "res://images/cards/CutePet.png");
    }
    private static BlockingPlayerChoiceContext ChoiceContext() => new();
    private static bool ModifyAmount(decimal __2, ref decimal __result)
    {
        _modifyCalls++;
        __result = __2 + _modifier;
        return false;
    }
    private static void CountAfter() => _afterCalls++;
}
