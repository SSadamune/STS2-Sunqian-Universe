using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Combat.HealthBars;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Degrade;

/// <summary>
/// Verifies that both optional interop paths degrade to the mod's original
/// behavior when their target mods are not loaded. These tests are compiled
/// only for the normal test payload, whose only external dependency is RitsuLib.
/// </summary>
public sealed class OptionalModDegradeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("optional-mod-interop-degrade");

    [Fact]
    public async Task Missing_DefenseAwareDebuffPreview_keeps_vanilla_Poison_and_basic_Burning_forecasts()
    {
        AssertOptionalModsUnavailable();
        Creature enemy = EnemyAt(0);
        await ApplyPower<BurningPower>(enemy, 4);
        await ApplyPower<PoisonPower>(enemy, 4);
        await ApplyPower<SlipperyPower>(enemy, 1);
        var burning = Assert.IsType<BurningPower>(enemy.GetPower<BurningPower>());
        var poison = Assert.IsType<PoisonPower>(enemy.GetPower<PoisonPower>());
        HealthBarForecastSegment[] segments =
            burning.GetHealthBarForecastSegments(new(enemy)).ToArray();

        Assert.Equal(4, poison.CalculateTotalDamageNextTurn());
        Assert.Equal(4, Assert.Single(segments).Amount);
    }

    [Fact]
    public async Task Missing_Yjcm_keeps_Shangfanggu_and_GoodFire_fully_functional()
    {
        AssertOptionalModsUnavailable();
        var enemy = EnemyAt(0);
        var shangfanggu = Upgrade(await AddToHand<ShangfangguSigh>());

        Assert.DoesNotContain(
            "[purple]",
            shangfanggu.GetDescriptionForPile(PileType.Hand));
        _ = shangfanggu.HoverTips.ToArray();

        await Play(shangfanggu);

        var goodFire = Assert.IsType<GoodFirePower>(
            Player.Creature.GetPower<GoodFirePower>());
        Assert.Equal(ShangfangguSigh.UpgradedBonusPercent, goodFire.Amount);
        _ = goodFire.HoverTips.ToArray();

        await Play(await AddToHand<FlameStrike>(), enemy);

        int expectedBurningPerApplication = (int)decimal.Floor(
            FlameStrike.BaseBurning
            * (1m + ShangfangguSigh.UpgradedBonusPercent / 100m));
        int expectedBurning = expectedBurningPerApplication * 2;
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, expectedBurning);
    }

    [Fact]
    public async Task Missing_Yjcm_keeps_Tinder_functional_for_Burning()
    {
        AssertOptionalModsUnavailable();
        const int tinderAmount = 3;
        await ApplyPower<TinderPower>(Player.Creature, tinderAmount);
        var tinder = Assert.IsType<TinderPower>(
            Player.Creature.GetPower<TinderPower>());

        Assert.DoesNotContain("[purple]", FormatPowerDescription(tinder));
        _ = tinder.HoverTips.ToArray();

        var enemy = EnemyAt(0);
        await Play(await AddToHand<FlameStrike>(), enemy);

        CardTestAssertions.AssertPowerAmount<BurningPower>(
            enemy,
            (FlameStrike.BaseBurning + tinderAmount) * 2);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Missing_Newsanguo_keeps_Wine_GrandGoblet_and_their_power_functional()
    {
        AssertOptionalModsUnavailable();
        string vigorTitle = ModelDb.Power<VigorPower>().Title.GetFormattedText();
        var grandGoblet = await AddToHand<GrandGoblet>();
        var wine = await AddToHand<Wine>();

        AssertUsesOriginalVigorText(grandGoblet, vigorTitle);
        AssertUsesOriginalVigorText(wine, vigorTitle);
        _ = grandGoblet.HoverTips.ToArray();
        _ = wine.HoverTips.ToArray();

        int dazedBefore = PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count();
        await Play(grandGoblet);
        await Play(wine);

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            Wine.BaseVigor);
        Assert.Equal(
            dazedBefore + 1,
            PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count());

        var grandGobletPower = Assert.IsType<GrandGobletPower>(
            Player.Creature.GetPower<GrandGobletPower>());
        _ = grandGobletPower.HoverTips.ToArray();
        int energyBeforeTrigger = Player.PlayerCombatState!.Energy;

        await grandGobletPower.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Player.Creature.CombatState!);

        Assert.Equal(
            energyBeforeTrigger + GrandGoblet.EnergyGain,
            Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            Wine.BaseVigor + GrandGoblet.BaseVigor);
    }

    private static void AssertOptionalModsUnavailable()
    {
        Assert.False(YjcmPublicApiInterop.IsReady);
        Assert.False(NewsanguoPublicApiInterop.IsReady);
        Assert.DoesNotContain(
            AppDomain.CurrentDomain.GetAssemblies(),
            assembly => string.Equals(
                assembly.GetName().Name,
                "Yjcm",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            AppDomain.CurrentDomain.GetAssemblies(),
            assembly => string.Equals(
                assembly.GetName().Name,
                "newsanguo",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            AppDomain.CurrentDomain.GetAssemblies(),
            assembly => string.Equals(
                assembly.GetName().Name,
                "DefenseAwareDebuffPreview",
                StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertUsesOriginalVigorText(CardModel card, string vigorTitle)
    {
        string description = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains(vigorTitle, description);
        Assert.DoesNotContain("[purple]", description);
    }

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}
