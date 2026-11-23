using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

internal static class TestYjcmGoodFireInterop
{
    public static bool Enabled { get; set; }

    public static bool IsYjcmCharacter { get; set; } = true;
}

[HarmonyPatch(typeof(YjcmPublicApiInterop), "get_IsReady")]
internal static class TestYjcmGoodFireReadyPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!TestYjcmGoodFireInterop.Enabled)
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(YjcmPublicApiInterop), nameof(YjcmPublicApiInterop.IsYjcmCharacter))]
internal static class TestYjcmGoodFireCharacterPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!TestYjcmGoodFireInterop.Enabled)
        {
            return true;
        }

        __result = TestYjcmGoodFireInterop.IsYjcmCharacter;
        return false;
    }
}

[HarmonyPatch(typeof(YjcmPublicApiInterop), nameof(YjcmPublicApiInterop.IsHuoGongPower))]
internal static class TestYjcmGoodFirePowerPatch
{
    private static bool Prefix(PowerModel? power, ref bool __result)
    {
        if (!TestYjcmGoodFireInterop.Enabled)
        {
            return true;
        }

        __result = power is VulnerablePower;
        return false;
    }
}

public sealed class ShangfangguSighTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("shangfanggu-sigh");

    [Fact]
    public async Task Good_fire_amplifies_enemy_burning_but_not_self_or_friendly_pet_burning()
    {
        var context = new BlockingPlayerChoiceContext();
        var card = Upgrade(await AddToHand<ShangfangguSigh>());
        Assert.Contains("给予敌方", card.GetDescriptionForPile(PileType.Hand));
        await Play(card);
        var pet = (await OstyCmd.Summon(context, Player, 5, null)).Creature!;
        await PowerCmd.Apply<BurningPower>(context, Player.Creature, 3, Player.Creature, null);
        await PowerCmd.Apply<BurningPower>(context, pet, 3, Player.Creature, null);
        await PowerCmd.Apply<BurningPower>(context, EnemyAt(0), 3, Player.Creature, null);
        Assert.Equal(3, Player.Creature.GetPower<BurningPower>()!.Amount);
        Assert.Equal(3, pet.GetPower<BurningPower>()!.Amount);
        Assert.Equal(7, EnemyAt(0).GetPower<BurningPower>()!.Amount);
        Assert.Equal(1m, Player.Creature.GetPower<GoodFirePower>()!.ModifyPowerAmountGivenMultiplicative(
            ModelDb.Power<BurningPower>(), Player.Creature, 3, null, card));
        Assert.Equal("夷陵余烬", ModelDb.Power<PendingGoodFirePower>().Title.GetFormattedText());
    }

    [Fact]
    public async Task Good_fire_also_multiplies_yjcm_fire_attack_for_yjcm_characters()
    {
        var card = Upgrade(await AddToHand<ShangfangguSigh>());

        Assert.False(YjcmPublicApiInterop.IsReady);
        Assert.DoesNotContain(
            card.HoverTips,
            tip => tip is HoverTip textTip
                && textTip.Title == SquCommonL10n.YjcmGoodFireHoverTitle()
                    .GetFormattedText());
        Assert.DoesNotContain(
            "[purple]",
            card.GetDescriptionForPile(PileType.Hand));

        await Play(card);
        var goodFire = Assert.IsType<GoodFirePower>(
            Player.Creature.GetPower<GoodFirePower>());

        decimal expectedMultiplier = 1m + ShangfangguSigh.UpgradedBonusPercent / 100m;
        Assert.Equal(
            expectedMultiplier,
            goodFire.ModifyPowerAmountGivenMultiplicative(
                ModelDb.Power<BurningPower>(),
                Player.Creature,
                3m,
                EnemyAt(0),
                card));

        Assert.Equal(
            1m,
            goodFire.ModifyPowerAmountGivenMultiplicative(
                ModelDb.Power<VulnerablePower>(),
                Player.Creature,
                3m,
                EnemyAt(0),
                card));

        TestYjcmGoodFireInterop.Enabled = true;
        try
        {
            Assert.Equal(
                expectedMultiplier,
                goodFire.ModifyPowerAmountGivenMultiplicative(
                    ModelDb.Power<VulnerablePower>(),
                    Player.Creature,
                    3m,
                    EnemyAt(0),
                    card));

            Assert.Equal(
                1m,
                goodFire.ModifyPowerAmountGivenMultiplicative(
                    ModelDb.Power<WeakPower>(),
                    Player.Creature,
                    3m,
                    EnemyAt(0),
                    card));
        }
        finally
        {
            TestYjcmGoodFireInterop.Enabled = false;
            TestYjcmGoodFireInterop.IsYjcmCharacter = true;
        }
    }

    [Fact]
    public async Task Loaded_yjcm_does_not_change_effect_or_hover_text_for_other_characters()
    {
        TestYjcmGoodFireInterop.Enabled = true;
        TestYjcmGoodFireInterop.IsYjcmCharacter = false;
        try
        {
            var card = Upgrade(await AddToHand<ShangfangguSigh>());
            Assert.DoesNotContain(
                card.HoverTips,
                tip => tip is HoverTip textTip
                    && textTip.Title == SquCommonL10n.YjcmGoodFireHoverTitle()
                        .GetFormattedText());
            Assert.DoesNotContain(
                "[purple]",
                card.GetDescriptionForPile(PileType.Hand));

            await Play(card);

            var goodFire = Assert.IsType<GoodFirePower>(
                Player.Creature.GetPower<GoodFirePower>());
            decimal expectedBurningMultiplier =
                1m + ShangfangguSigh.UpgradedBonusPercent / 100m;
            Assert.Equal(
                expectedBurningMultiplier,
                goodFire.ModifyPowerAmountGivenMultiplicative(
                    ModelDb.Power<BurningPower>(),
                    Player.Creature,
                    3m,
                    EnemyAt(0),
                    card));
            Assert.Equal(
                1m,
                goodFire.ModifyPowerAmountGivenMultiplicative(
                    ModelDb.Power<VulnerablePower>(),
                    Player.Creature,
                    3m,
                    EnemyAt(0),
                    card));
        }
        finally
        {
            TestYjcmGoodFireInterop.Enabled = false;
            TestYjcmGoodFireInterop.IsYjcmCharacter = true;
        }
    }
}
