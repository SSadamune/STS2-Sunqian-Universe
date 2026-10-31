using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using Squ.Relics;
using STS2RitsuLib.Combat.HealthBars;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.DefenseAwareDebuffPreview;

/// <summary>
/// Compiled only by the dedicated test mode, which requires the optional mod to
/// be present in the TestTheSpire payload.
/// </summary>
public sealed class DefenseAwareDebuffPreviewInteropTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("defense-aware-debuff-preview-interop");

    [Fact]
    public void Dedicated_mode_loads_DefenseAwareDebuffPreview()
    {
        Assert.NotNull(GetPreviewAssembly());
    }

    [Fact]
    public async Task Enemy_Burning_then_Poison_share_Slippery_once_in_power_order()
    {
        Creature enemy = EnemyAt(0);
        BurningPower burning = await ApplyAndGet<BurningPower>(enemy, 4);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(enemy, 4);
        await ApplyPower<SlipperyPower>(enemy, 1);
        AssertPowerOrder(enemy, burning, poison);

        int hpBefore = enemy.CurrentHp;
        HealthBarForecastSegment burningSegment = GetBurningPreview(burning);
        HealthBarForecastSegment poisonSegment = GetOrderedPoisonPreview(poison);
        Assert.Equal(1, burningSegment.Amount);
        Assert.Equal(4, poisonSegment.Amount);
        Assert.True(burningSegment.Order < poisonSegment.Order);
        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Equal(1, enemy.GetPower<SlipperyPower>()?.Amount);
    }

    [Fact]
    public async Task Enemy_Poison_then_Burning_share_Buffer_once_in_power_order()
    {
        Creature enemy = EnemyAt(0);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(enemy, 5);
        BurningPower burning = await ApplyAndGet<BurningPower>(enemy, 4);
        await ApplyPower<BufferPower>(enemy, 1);
        AssertPowerOrder(enemy, poison, burning);

        int hpBefore = enemy.CurrentHp;
        // HealthBarForecastSegment is a struct: SingleOrDefault returns a zero
        // struct for an empty sequence, not a nullable with no value.
        Assert.Empty(GetOrderedPoisonPreviews(poison));
        HealthBarForecastSegment burningSegment = GetBurningPreview(burning);
        Assert.Equal(4, burningSegment.Amount);
        Assert.Equal(1, burningSegment.Order);
        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Equal(1, enemy.GetPower<BufferPower>()?.Amount);
    }

    [Fact]
    public async Task Multi_hit_Poison_consumes_preview_defenses_per_hit_without_mutation()
    {
        Creature enemy = EnemyAt(0);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(enemy, 3);
        await ApplyPower<BufferPower>(enemy, 1);
        await ApplyPower<AccelerantPower>(Player.Creature, 1);
        int hpBefore = enemy.CurrentHp;

        Assert.Equal(5, poison.CalculateTotalDamageNextTurn());
        Assert.Equal(2, CalculatePoisonUiPreview(poison));
        Assert.Equal(5, poison.CalculateTotalDamageNextTurn());
        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Equal(1, enemy.GetPower<BufferPower>()?.Amount);
        Assert.Equal(3, poison.Amount);
    }

    [Fact]
    public async Task Enemy_turn_start_Burning_and_Poison_reset_defenses_before_they_share_them()
    {
        Creature enemy = EnemyAt(0);
        await ApplyPower<HardenedShellPower>(enemy, 20);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            enemy,
            12m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await CreatureCmd.GainBlock(enemy, 6m, ValueProp.Unpowered, null);
        BurningPower burning = await ApplyAndGet<BurningPower>(enemy, 10);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(enemy, 15);
        AssertPowerOrder(enemy, burning, poison);
        HardenedShellPower shell = Assert.IsType<HardenedShellPower>(
            enemy.GetPower<HardenedShellPower>());
        Assert.Equal(8, shell.DisplayAmount);

        int hpBefore = enemy.CurrentHp;
        HealthBarForecastSegment burningSegment = GetBurningPreview(burning);
        HealthBarForecastSegment poisonSegment = GetOrderedPoisonPreview(poison);

        Assert.Equal(10, burningSegment.Amount);
        Assert.Equal(10, poisonSegment.Amount);
        Assert.True(burningSegment.Order < poisonSegment.Order);
        Assert.Equal(6, enemy.Block);
        Assert.Equal(8, shell.DisplayAmount);
        Assert.Equal(hpBefore, enemy.CurrentHp);
    }

    [Fact]
    public async Task Player_turn_end_Burning_keeps_current_defenses_while_Poison_uses_the_next_turn()
    {
        Creature player = Player.Creature;
        await ApplyPower<HardenedShellPower>(player, 20);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            player,
            12m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await CreatureCmd.GainBlock(player, 6m, ValueProp.Unpowered, null);
        BurningPower burning = await ApplyAndGet<BurningPower>(player, 10);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(player, 15);
        HardenedShellPower shell = Assert.IsType<HardenedShellPower>(
            player.GetPower<HardenedShellPower>());
        Assert.Equal(8, shell.DisplayAmount);

        int hpBefore = player.CurrentHp;
        Assert.Equal(4, CalculateBurningPreview(burning));
        Assert.Equal(15, CalculatePoisonUiPreview(poison));
        Assert.Equal(6, player.Block);
        Assert.Equal(8, shell.DisplayAmount);
        Assert.Equal(hpBefore, player.CurrentHp);
    }

    [Fact]
    public async Task Player_and_pet_separate_turn_start_Poison_from_turn_end_Burning()
    {
        await AssertSeparateTriggerBatches(Player.Creature);

        var summon = await OstyCmd.Summon(
            new BlockingPlayerChoiceContext(),
            Player,
            12m,
            null);
        Creature pet = summon.Creature
            ?? throw new InvalidOperationException("Osty was not summoned.");
        Assert.True(pet.IsPet);

        await AssertSeparateTriggerBatches(pet);
    }

    [Fact]
    public async Task Preview_preserves_hp_block_power_layers_and_relic_state()
    {
        Creature player = Player.Creature;
        PoisonPower poison = await ApplyAndGet<PoisonPower>(player, 4);
        BurningPower burning = await ApplyAndGet<BurningPower>(player, 4);
        await ApplyPower<BufferPower>(player, 1);
        await ApplyPower<SlipperyPower>(player, 1);
        await CreatureCmd.GainBlock(player, 6m, ValueProp.Unpowered, null);

        var relic = (NeverHadTheseRelic)ModelDb.Relic<NeverHadTheseRelic>().ToMutable();
        relic.Owner = Player;
        relic.RemovedFamilyCards = 2;
        relic.RemovedCutePets = 1;
        Player.AddRelicInternal(relic, silent: true);

        int hpBefore = player.CurrentHp;
        decimal blockBefore = player.Block;
        (PowerModel Power, int Amount)[] powersBefore = player.Powers
            .Select(power => (power, power.Amount))
            .ToArray();
        int familyCardsBefore = relic.RemovedFamilyCards;
        int cutePetsBefore = relic.RemovedCutePets;

        _ = CalculatePoisonUiPreview(poison);
        _ = CalculateBurningPreview(burning);

        Assert.Equal(hpBefore, player.CurrentHp);
        Assert.Equal(blockBefore, player.Block);
        Assert.Equal(powersBefore, player.Powers
            .Select(power => (power, power.Amount))
            .ToArray());
        Assert.Contains(relic, Player.Relics);
        Assert.Equal(familyCardsBefore, relic.RemovedFamilyCards);
        Assert.Equal(cutePetsBefore, relic.RemovedCutePets);
    }

    [Fact]
    public async Task Poison_outside_health_bar_scope_always_retains_vanilla_result()
    {
        Creature enemy = EnemyAt(0);
        PoisonPower poison = await ApplyAndGet<PoisonPower>(enemy, 10);
        await ApplyPower<BufferPower>(enemy, 1);

        Assert.Equal(10, poison.CalculateTotalDamageNextTurn());
        Assert.Equal(0, CalculatePoisonUiPreview(poison));
        Assert.Equal(10, poison.CalculateTotalDamageNextTurn());
        Assert.Equal(1, enemy.GetPower<BufferPower>()?.Amount);
    }

    private async Task AssertSeparateTriggerBatches(Creature target)
    {
        PoisonPower poison = await ApplyAndGet<PoisonPower>(target, 4);
        BurningPower burning = await ApplyAndGet<BurningPower>(target, 4);
        await ApplyPower<SlipperyPower>(target, 1);
        int hpBefore = target.CurrentHp;

        Assert.Equal(1, CalculatePoisonUiPreview(poison));
        Assert.Equal(1, CalculateBurningPreview(burning));
        Assert.Equal(hpBefore, target.CurrentHp);
        Assert.Equal(1, target.GetPower<SlipperyPower>()?.Amount);
    }

    private async Task<TPower> ApplyAndGet<TPower>(Creature target, int amount)
        where TPower : PowerModel
    {
        await ApplyPower<TPower>(target, amount);
        return Assert.IsType<TPower>(target.GetPower<TPower>());
    }

    private static int CalculateBurningPreview(BurningPower burning)
    {
        var segments = burning.GetHealthBarForecastSegments(new(burning.Owner)).ToArray();
        Assert.InRange(segments.Length, 0, 1);
        // Zero predicted HP loss intentionally has no visible health-bar segment.
        return segments.Sum(segment => segment.Amount);
    }

    private static HealthBarForecastSegment GetBurningPreview(BurningPower burning) =>
        Assert.Single(burning.GetHealthBarForecastSegments(new(burning.Owner)));

    private static HealthBarForecastSegment GetOrderedPoisonPreview(PoisonPower poison) =>
        Assert.Single(GetOrderedPoisonPreviews(poison));

    private static IEnumerable<HealthBarForecastSegment> GetOrderedPoisonPreviews(
        PoisonPower poison)
    {
        Type sourceType = typeof(BurningPower).Assembly.GetType(
            "Squ.Combat.OrderedPoisonHealthBarForecastSource")
            ?? throw new InvalidOperationException("Ordered Poison forecast source was not found.");
        var source = Assert.IsAssignableFrom<IHealthBarForecastSource>(
            Activator.CreateInstance(sourceType, nonPublic: true));

        return source.GetHealthBarForecastSegments(new(poison.Owner));
    }

    private static int CalculatePoisonUiPreview(PoisonPower poison)
    {
        Type squScope = typeof(BurningPower).Assembly.GetType(
            "Squ.Patches.PoisonHealthBarPreviewScopePatch")
            ?? throw new InvalidOperationException("Sunqian Poison UI scope was not found.");
        Type previewScope = GetPreviewAssembly().GetType(
            "DefenseAwareDebuffPreview.Patches.PoisonHealthBarPreviewScopePatch")
            ?? throw new InvalidOperationException("DefenseAwareDebuffPreview Poison UI scope was not found.");
        Type[] scopes = [previewScope, squScope];

        foreach (Type scope in scopes)
        {
            GetScopeMethod(scope, "EnterPreviewScope").Invoke(null, null);
        }

        try
        {
            return poison.CalculateTotalDamageNextTurn();
        }
        finally
        {
            for (int index = scopes.Length - 1; index >= 0; index--)
            {
                GetScopeMethod(scopes[index], "ExitPreviewScope").Invoke(
                    null,
                    new object?[] { null });
            }
        }
    }

    private static MethodInfo GetScopeMethod(Type scope, string name) =>
        scope.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(scope.FullName, name);

    private static Assembly GetPreviewAssembly() =>
        AppDomain.CurrentDomain.GetAssemblies().Single(assembly => string.Equals(
            assembly.GetName().Name,
            "DefenseAwareDebuffPreview",
            StringComparison.OrdinalIgnoreCase));

    private static void AssertPowerOrder(
        Creature target,
        PowerModel first,
        PowerModel second)
    {
        PowerModel[] powers = target.Powers.ToArray();
        Assert.True(
            Array.IndexOf(powers, first) < Array.IndexOf(powers, second),
            $"Expected {first.GetType().Name} before {second.GetType().Name}.");
    }
}
