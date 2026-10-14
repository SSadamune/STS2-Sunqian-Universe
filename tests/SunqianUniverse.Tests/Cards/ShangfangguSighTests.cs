using HarmonyLib;
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
            SquCommonL10n.YjcmGoodFireCombatNote().GetFormattedText(),
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
                SquCommonL10n.YjcmGoodFireCombatNote().GetFormattedText(),
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
