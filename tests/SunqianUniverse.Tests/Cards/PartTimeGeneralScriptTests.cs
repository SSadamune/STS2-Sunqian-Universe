using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PartTimeGeneralScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("part-time-general");

    [Theory]
    [InlineData(false, 40, 40, 20)]
    [InlineData(true, 10, 60, 30)]
    public void Enchantment_tier_probabilities_match_the_script_upgrade(bool upgraded, int common, int uncommon, int rare)
    {
        int[] tiers = Enumerable.Range(0, 100)
            .Select(roll => ScriptPartTimeGeneralPower.GetEnchantmentTier(roll, upgraded)).ToArray();
        Assert.Equal(common, tiers.Count(tier => tier == 1));
        Assert.Equal(uncommon, tiers.Count(tier => tier == 2));
        Assert.Equal(rare, tiers.Count(tier => tier == 3));
    }

    [Fact]
    public void Slither_has_thirty_percent_priority_only_for_high_fixed_cost_cards()
    {
        var rng = Player.RunState.Rng.CombatCardGeneration;
        foreach (bool upgraded in new[] { false, true })
        {
            int slitherCount = Enumerable.Range(0, 10000).Count(_ =>
                ScriptPartTimeGeneralPower.RollEnchantment(ModelDb.Card<Bludgeon>(), upgraded, rng)
                    .EnchantmentType == typeof(Slither));
            Assert.InRange(slitherCount, 2800, 3200);
            for (int i = 0; i < 100; i++)
            {
                Assert.NotEqual(typeof(Slither), ScriptPartTimeGeneralPower.RollEnchantment(
                    ModelDb.Card<StrikeIronclad>(), upgraded, rng).EnchantmentType);
                Assert.NotEqual(typeof(Slither), ScriptPartTimeGeneralPower.RollEnchantment(
                    ModelDb.Card<Whirlwind>(), upgraded, rng).EnchantmentType);
            }
        }
    }

    [Fact]
    public async Task Upgraded_source_bonus_survives_normal_reinforcement_and_serialization_without_upgrading_record_ui()
    {
        var source = await AddToHand<PartTimeGeneralScript>();
        var upgraded = Upgrade(await AddToHand<PartTimeGeneralScript>());
        var record = (PartTimeGeneralScriptRecord)ModelDb.Card<PartTimeGeneralScriptRecord>().ToMutable();
        record.InitializeFrom(source, 1);
        Assert.False(record.SourceUpgraded);
        record.ReinforceFrom(upgraded, 1);
        record.ReinforceFrom(source, 1);
        Assert.True(record.SourceUpgraded);
        Assert.False(record.IsUpgraded);
        Assert.False(record.HasUpgradedVersion);
        var restored = Assert.IsType<PartTimeGeneralScriptRecord>(CardModel.FromSerializable(record.ToSerializable()));
        Assert.True(restored.SourceUpgraded);
        Assert.False(restored.IsUpgraded);
    }

    [Fact]
    public async Task Reselecting_the_active_general_resolves_expiration_before_reactivation()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(2, Player);
        await PlayAndActivateScript(await AddToHand<PartTimeGeneralScript>(), EnemyAt(0));
        var oldPower = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        var record = Assert.Single(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
            .OfType<PartTimeGeneralScriptRecord>());
        int recordCount = record.RecordedCards.Count;
        Assert.True(recordCount > 0);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(record)
            .ThenSelect(options =>
            {
                Assert.Null(Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
                Assert.Equal(recordCount, options.Count);
                Assert.All(options, card => Assert.NotNull(card.Enchantment));
                selected = options[0];
                return [selected];
            });
        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(selected);
        AssertInPile(selected, PileType.Hand);
        Assert.NotSame(oldPower, Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        AssertOnlyScript<ScriptPartTimeGeneralPower>();
        Assert.Equal(recordCount, record.RecordedCards.Count);
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
        Assert.False(relic.CanReselectFromScriptCollection());
    }

    [Fact]
    public async Task Expiring_script_offers_recorded_strikes_and_adds_the_selected_copy_to_hand()
    {
        int hp = EnemyAt(0).CurrentHp;
        await PlayAndActivateScript(
            Upgrade(await AddToHand<PartTimeGeneralScript>()),
            EnemyAt(0));
        Assert.True(EnemyAt(0).CurrentHp < hp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        var record = Assert.Single(
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<PartTimeGeneralScriptRecord>());
        int recordsBeforeExpiration = record.RecordedCards.Count;
        string preview = record.GetDescriptionForPile(record.Pile!.Type);
        Assert.Contains("从以下牌（具有随机[gold]附魔[/gold]）中获得一张：\n", preview);
        Assert.DoesNotContain("以此法打出过：", preview);
        Assert.False(string.IsNullOrWhiteSpace(
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString()));
        await PlayerCmd.SetEnergy(3, Player);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
            {
                selected = options[0];
                Assert.All(options, option => Assert.NotNull(option.Enchantment));
                return [selected];
            });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<ServantScript>());
        }

        AssertAllSelectionsConsumed(selector);
        CardModel chosen = Assert.IsAssignableFrom<CardModel>(selected);
        AssertInPile(chosen, PileType.Hand);
        Assert.True(chosen.IsUpgraded);
        Assert.Equal(chosen.EnergyCost.Canonical, chosen.EnergyCost.GetResolved());
        Assert.NotNull(chosen.Enchantment);
        Assert.Equal(recordsBeforeExpiration, record.RecordedCards.Count);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Zero_energy_plays_no_strikes_but_still_installs_an_empty_script()
    {
        await PlayerCmd.SetEnergy(0, Player);
        int hp = EnemyAt(0).CurrentHp;

        await PlayAndActivateScript(
            await AddToHand<PartTimeGeneralScript>(),
            EnemyAt(0));

        Assert.Equal(hp, EnemyAt(0).CurrentHp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        var record = Assert.Single(
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<PartTimeGeneralScriptRecord>());
        Assert.Empty(record.RecordedCards);
        Assert.Equal(
            "—",
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString());
        Assert.DoesNotContain(
            "—",
            record.GetDescriptionForPile(record.Pile!.Type));

        await PlayerCmd.SetEnergy(2, Player);
        await PlayAndActivateScript(await AddToHand<ServantScript>());

        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Nested_strike_drawn_by_pommel_and_autoplayed_by_hellraiser_keeps_supreme_general_vigor()
    {
        const int vigor = 5;
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        await ApplyPower<HellraiserPower>(Player.Creature, 1);
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        StrikeIronclad drawnStrike = await AddToDrawPileTop<StrikeIronclad>();
        await PlayerCmd.SetEnergy(1, Player);
        int hp = EnemyAt(0).CurrentHp;
        int expectedDamage = (int)(
            ModelDb.Card<PommelStrike>().DynamicVars.Damage.BaseValue
            + drawnStrike.DynamicVars.Damage.BaseValue
            + vigor * 2);

        MethodInfo chooseStrikes = AccessTools.DeclaredMethod(
                typeof(PartTimeGeneralScript),
                "ChooseStrikeCards")
            ?? throw new InvalidOperationException("ChooseStrikeCards was not found.");
        var harmony = new Harmony("squ.tests.force-pommel-strike");
        harmony.Patch(
            chooseStrikes,
            prefix: new HarmonyMethod(typeof(PartTimeGeneralScriptTests), nameof(ForcePommelStrike)));
        try
        {
            await PlayAndActivateScript(
                await AddToHand<PartTimeGeneralScript>(),
                EnemyAt(0));
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        Assert.True((Player.Creature.GetPower<VigorPower>()?.Amount ?? 0m) == 0m);
        AssertInPile(drawnStrike, PileType.Discard);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.Contains(
            ModelDb.Card<PommelStrike>().Title,
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString());
    }

    [Fact]
    public async Task Repeated_plays_keep_duplicate_records_and_each_selection_rerolls_enchantments()
    {
        MethodInfo chooseStrikes = AccessTools.DeclaredMethod(
                typeof(PartTimeGeneralScript),
                "ChooseStrikeCards")
            ?? throw new InvalidOperationException("ChooseStrikeCards was not found.");
        var harmony = new Harmony("squ.tests.force-duplicate-pommel-strikes");
        harmony.Patch(
            chooseStrikes,
            prefix: new HarmonyMethod(typeof(PartTimeGeneralScriptTests), nameof(ForcePommelStrike)));
        try
        {
            await PlayerCmd.SetEnergy(1, Player);
            await PlayAndActivateScript(
                await AddToHand<PartTimeGeneralScript>(),
                EnemyAt(0));
            await PlayerCmd.SetEnergy(1, Player);
            await PlayAndActivateScript(
                await AddToHand<PartTimeGeneralScript>(),
                EnemyAt(0));
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        var record = Assert.Single(
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<PartTimeGeneralScriptRecord>());
        Assert.Equal(2, record.RecordedCards.Count);
        Assert.Contains(
            ModelDb.Card<PommelStrike>().Title,
            record.GetDescriptionForPile(record.Pile!.Type));

        CardModel? selected = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
            {
                Assert.Equal(2, options.Count);
                Assert.All(options, option => Assert.NotNull(option.Enchantment));
                selected = options[0];
                return [selected];
            });
        await PlayerCmd.SetEnergy(3, Player);
        using (UseCardSelector(selector))
        {
            await PlayAndActivateScript(await AddToHand<ServantScript>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, record.RecordedCards.Count);
        CardModel returned = Assert.IsAssignableFrom<CardModel>(selected);
        AssertInPile(returned, PileType.Hand);
        Assert.Equal(returned.EnergyCost.Canonical, returned.EnergyCost.GetResolved());

        // Reopen the same record repeatedly: previous choices remain available,
        // each screen owns fresh copies, and random enchantments never enter the record.
        var observed = new HashSet<string>();
        var power = (ScriptPartTimeGeneralPower)ModelDb.Power<ScriptPartTimeGeneralPower>().ToMutable();
        power.AttachRecord(record);
        MethodInfo choose = AccessTools.DeclaredMethod(typeof(ScriptPartTimeGeneralPower), "ChooseRecordedCard")!;
        for (int i = 0; i < 12; i++)
        {
            var repeatSelector = new ScriptedCardSelector().ThenSelect(options =>
            {
                Assert.Equal(2, options.Count);
                Assert.All(options, option =>
                {
                    Assert.Equal(returned.Id, option.Id);
                    Assert.NotSame(returned, option);
                    Assert.NotSame(returned.Enchantment, option.Enchantment);
                    Assert.NotNull(option.Enchantment);
                    observed.Add($"{option.Enchantment.GetType().Name}:{option.Enchantment.Amount}");
                });
                return [options[0]];
            });
            using (UseCardSelector(repeatSelector))
            {
                await (Task)choose.Invoke(power, [Player.Creature.CombatState!, Player])!;
            }
            AssertAllSelectionsConsumed(repeatSelector);
        }
        Assert.True(observed.Count > 1);
        Assert.Equal(2, record.RecordedCards.Count);
        Assert.All(record.RecordedCards, saved => Assert.Null(CardModel.FromSerializable(saved).Enchantment));
    }

    private static bool ForcePommelStrike(int count, ref List<CardModel> __result)
    {
        __result = count <= 0 ? [] : [ModelDb.Card<PommelStrike>()];
        return false;
    }
}
