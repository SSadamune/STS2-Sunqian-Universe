using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Potions;
using Squ.Character;
using Squ.Cards;
using Squ.Powers;
using Squ.RunData;
using Squ.Settings;
using STS2RitsuLib.Data;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Settings;

public sealed class ContentSettingsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("content-settings");

    [Fact]
    public void Settings_defaults_match_the_documented_content_policy()
    {
        var settings = new SquSettings();

        Assert.Equal(50, settings.SfxVolumePercent);
        Assert.Equal("cybertron", settings.PreviewSfxId);
        Assert.Equal("sunqian", settings.PreferredActorVariant);
        Assert.Equal(
            NeutralContentPermissionMode.WhenModCharacterPresent,
            settings.NeutralPotionModification);
        Assert.Equal(
            NeutralContentPermissionMode.Allow,
            settings.NeutralCardRegistration);
    }

    [Fact]
    public void Volume_curve_clamps_both_ends_and_is_strictly_increasing_above_mute()
    {
        Assert.Equal(0f, SquSettings.PercentToLinearMultiplier(-1));
        Assert.Equal(0f, SquSettings.PercentToLinearMultiplier(0));
        Assert.Equal(
            SquSettings.PeakSfxGain,
            SquSettings.PercentToLinearMultiplier(100),
            precision: 5);
        Assert.Equal(
            SquSettings.PeakSfxGain,
            SquSettings.PercentToLinearMultiplier(101),
            precision: 5);

        float previous = 0f;
        for (int percent = 1; percent <= 100; percent++)
        {
            float current = SquSettings.PercentToLinearMultiplier(percent);
            Assert.True(
                current > previous,
                $"Expected the volume curve to increase at {percent}%, but {current} <= {previous}.");
            previous = current;
        }
    }

    [Fact]
    public void Fifty_percent_is_the_zero_db_reference_point()
    {
        float expectedUnityGain = SquSettings.PeakSfxGain
            / MathF.Pow(10f, SquSettings.MaxBoostDb / 20f);

        Assert.Equal(
            expectedUnityGain,
            SquSettings.PercentToLinearMultiplier(50),
            precision: 5);
    }

    [Fact]
    public void Runtime_volume_reads_the_latest_setting_and_clamps_corrupt_values()
    {
        SquSettings local = GetLocalSettings();
        int original = local.SfxVolumePercent;

        try
        {
            local.SfxVolumePercent = -20;
            Assert.Equal(0f, SquSettings.SfxLinearMultiplier);

            local.SfxVolumePercent = 100;
            Assert.Equal(SquSettings.PeakSfxGain, SquSettings.SfxLinearMultiplier, precision: 5);

            local.SfxVolumePercent = 120;
            Assert.Equal(SquSettings.PeakSfxGain, SquSettings.SfxLinearMultiplier, precision: 5);
        }
        finally
        {
            local.SfxVolumePercent = original;
        }
    }

    [Theory]
    [InlineData(NeutralContentPermissionMode.Allow, true)]
    [InlineData(NeutralContentPermissionMode.WhenModCharacterPresent, true)]
    [InlineData(NeutralContentPermissionMode.Deny, false)]
    public void Neutral_content_policies_follow_the_frozen_run_snapshot_when_sunqian_is_present(
        NeutralContentPermissionMode mode,
        bool expected)
    {
        WithLocalSettings(
            mode,
            mode,
            () =>
            {
                Assert.Equal(expected, NeutralCardRegistrationPolicy.ShouldInclude());
                Assert.Equal(
                    expected,
                    NeutralPotionModificationPolicy.ShouldApply(ModelDb.Potion<FirePotion>()));
            });
    }

    [Fact]
    public void Active_run_snapshot_is_read_only_and_does_not_follow_later_local_edits()
    {
        SquSettings local = GetLocalSettings();
        NeutralContentPermissionMode originalPotion = local.NeutralPotionModification;
        NeutralContentPermissionMode originalCard = local.NeutralCardRegistration;

        try
        {
            local.NeutralPotionModification = NeutralContentPermissionMode.Allow;
            local.NeutralCardRegistration = NeutralContentPermissionMode.Deny;
            RefreshSnapshot();

            Assert.True(ContentSettingsRunData.IsReadOnly);
            Assert.Equal(
                NeutralContentPermissionMode.Allow,
                ContentSettingsRunData.DisplayedPotionMode(local));
            Assert.Equal(
                NeutralContentPermissionMode.Deny,
                ContentSettingsRunData.DisplayedCardMode(local));

            Assert.False(ContentSettingsRunData.TryWriteLocalPotionMode(
                local,
                NeutralContentPermissionMode.Deny));
            Assert.False(ContentSettingsRunData.TryWriteLocalCardMode(
                local,
                NeutralContentPermissionMode.Allow));
            Assert.Equal(NeutralContentPermissionMode.Allow, local.NeutralPotionModification);
            Assert.Equal(NeutralContentPermissionMode.Deny, local.NeutralCardRegistration);
        }
        finally
        {
            local.NeutralPotionModification = originalPotion;
            local.NeutralCardRegistration = originalCard;
            RefreshSnapshot();
        }
    }

    [Fact]
    public void Neutral_card_setting_filters_the_actual_colorless_reward_pool()
    {
        WithLocalSettings(
            NeutralContentPermissionMode.Allow,
            NeutralContentPermissionMode.Allow,
            () =>
            {
                Type[] allowed = GetAffectedUnlockedColorlessCards();
                Assert.Contains(typeof(TheArrogantFall), allowed);
                Assert.Contains(typeof(TheGrievingPrevail), allowed);
            });

        WithLocalSettings(
            NeutralContentPermissionMode.Allow,
            NeutralContentPermissionMode.Deny,
            () => Assert.Empty(GetAffectedUnlockedColorlessCards()));
    }

    [Fact]
    public async Task Neutral_potion_setting_switches_between_vanilla_and_modified_fire_potion_resolution()
    {
        var enemy = EnemyAt(0);

        await WithLocalSettingsAsync(
            NeutralContentPermissionMode.Deny,
            NeutralContentPermissionMode.Allow,
            async () =>
            {
                int hp = enemy.CurrentHp;
                await UseFirePotionDirectly(enemy);

                Assert.Equal(hp - 20, enemy.CurrentHp);
                CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, 0);
            });

        await WithLocalSettingsAsync(
            NeutralContentPermissionMode.Allow,
            NeutralContentPermissionMode.Allow,
            async () =>
            {
                int hp = enemy.CurrentHp;
                await UseFirePotionDirectly(enemy);

                Assert.Equal(hp - 2, enemy.CurrentHp);
                CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, 14);
            });
    }

    [Fact]
    public void Invalid_saved_modes_fall_back_to_each_settings_default()
    {
        WithLocalSettings(
            (NeutralContentPermissionMode)999,
            (NeutralContentPermissionMode)999,
            () =>
            {
                Assert.Equal(
                    SquSettings.DefaultNeutralPotionModificationMode,
                    ContentSettingsRunData.NeutralPotionModification);
                Assert.Equal(
                    SquSettings.DefaultNeutralCardRegistrationMode,
                    ContentSettingsRunData.NeutralCardRegistration);
            });
    }

    private void WithLocalSettings(
        NeutralContentPermissionMode potionMode,
        NeutralContentPermissionMode cardMode,
        Action assertion)
    {
        SquSettings local = GetLocalSettings();
        NeutralContentPermissionMode originalPotion = local.NeutralPotionModification;
        NeutralContentPermissionMode originalCard = local.NeutralCardRegistration;

        try
        {
            local.NeutralPotionModification = potionMode;
            local.NeutralCardRegistration = cardMode;
            RefreshSnapshot();
            assertion();
        }
        finally
        {
            local.NeutralPotionModification = originalPotion;
            local.NeutralCardRegistration = originalCard;
            RefreshSnapshot();
        }
    }

    private async Task WithLocalSettingsAsync(
        NeutralContentPermissionMode potionMode,
        NeutralContentPermissionMode cardMode,
        Func<Task> assertion)
    {
        SquSettings local = GetLocalSettings();
        NeutralContentPermissionMode originalPotion = local.NeutralPotionModification;
        NeutralContentPermissionMode originalCard = local.NeutralCardRegistration;

        try
        {
            local.NeutralPotionModification = potionMode;
            local.NeutralCardRegistration = cardMode;
            RefreshSnapshot();
            await assertion();
        }
        finally
        {
            local.NeutralPotionModification = originalPotion;
            local.NeutralCardRegistration = originalCard;
            RefreshSnapshot();
        }
    }

    private Type[] GetAffectedUnlockedColorlessCards() =>
        ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)
            .Where(card => card is TheArrogantFall or TheGrievingPrevail or KnowWrongDenyWrong)
            .Select(card => card.GetType())
            .ToArray();

    private async Task UseFirePotionDirectly(Creature target)
    {
        var potion = ModelDb.Potion<FirePotion>().ToMutable();
        potion.Owner = Player;
        MethodInfo onUse = typeof(FirePotion).GetMethod(
            "OnUse",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(typeof(FirePotion).FullName, "OnUse");
        var task = (Task?)onUse.Invoke(
            potion,
            [new BlockingPlayerChoiceContext(), target]);
        await (task ?? throw new InvalidOperationException("FirePotion.OnUse returned no task."));
        await WaitForIdle();
    }

    private static SquSettings GetLocalSettings() =>
        ModDataStore.For(SquMod.ModId).Get<SquSettings>(SquSettings.DataKey);

    private void RefreshSnapshot()
    {
        MethodInfo refresh = typeof(ContentSettingsRunData).GetMethod(
            "RefreshLoadedRunSnapshot",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(ContentSettingsRunData).FullName,
                "RefreshLoadedRunSnapshot");
        refresh.Invoke(null, [Player.RunState]);
    }
}

public sealed class ConditionalContentSettingsWithoutSunqianTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("conditional-content-settings-without-sunqian");

    [Fact]
    public void Character_conditional_policies_are_disabled_without_sunqian_in_multiplayer()
    {
        SquSettings local = ModDataStore.For(SquMod.ModId)
            .Get<SquSettings>(SquSettings.DataKey);
        NeutralContentPermissionMode originalPotion = local.NeutralPotionModification;
        NeutralContentPermissionMode originalCard = local.NeutralCardRegistration;

        try
        {
            local.NeutralPotionModification = NeutralContentPermissionMode.WhenModCharacterPresent;
            local.NeutralCardRegistration = NeutralContentPermissionMode.WhenModCharacterPresent;
            RefreshSnapshot();

            Assert.False(NeutralCardRegistrationPolicy.ShouldInclude());
            Assert.False(NeutralPotionModificationPolicy.ShouldApply(ModelDb.Potion<FirePotion>()));
        }
        finally
        {
            local.NeutralPotionModification = originalPotion;
            local.NeutralCardRegistration = originalCard;
            RefreshSnapshot();
        }
    }

    private void RefreshSnapshot()
    {
        MethodInfo refresh = typeof(ContentSettingsRunData).GetMethod(
            "RefreshLoadedRunSnapshot",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(ContentSettingsRunData).FullName,
                "RefreshLoadedRunSnapshot");
        refresh.Invoke(null, [Player.RunState]);
    }
}
