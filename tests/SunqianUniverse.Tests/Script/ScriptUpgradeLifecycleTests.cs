using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class ScriptUpgradeLifecycleTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-upgrade-lifecycle");

    [Theory]
    [InlineData(typeof(GruntScript))]
    [InlineData(typeof(ServantScript))]
    [InlineData(typeof(ChickenFootCheeseScript))]
    [InlineData(typeof(SunqianScript))]
    [InlineData(typeof(TwoWordPoetScript))]
    [InlineData(typeof(PartTimeGeneralScript))]
    public async Task Single_upgrade_is_retained_in_both_play_orders_and_after_serialization(Type sourceType)
    {
        var normal = (ScriptCardTemplate)await AddToHand(ModelDb.GetId(sourceType));
        var upgraded = Upgrade((ScriptCardTemplate)await AddToHand(ModelDb.GetId(sourceType)));
        foreach (bool upgradedFirst in new[] { false, true })
        {
            var record = normal.CreateScriptRecordPreview();
            record.InitializeFrom(upgradedFirst ? upgraded : normal, 1);
            record.UpgradeFrom(upgradedFirst ? normal : upgraded);
            record.UpgradeFrom(normal);
            record.UpgradeFrom(upgraded);
            Assert.True(record.CanUpgradeScript);
            Assert.False(record.UsesMaterialReuse);
            Assert.True(record.SourceUpgraded);
            Assert.Equal(1, record.CurrentUpgradeLevel);
            Assert.False(record.IsUpgradable);
            var restored = Assert.IsAssignableFrom<ScriptRecordCardTemplate>(CardModel.FromSerializable(record.ToSerializable()));
            Assert.True(restored.SourceUpgraded);
            Assert.Equal(1, restored.CurrentUpgradeLevel);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Material_reuse_expires_before_level_change_and_adds_one_or_two_levels(bool firstUpgraded)
    {
        var context = new BlockingPlayerChoiceContext();
        var normal = await AddToHand<ExecutionCommanderScript>();
        var upgraded = Upgrade(await AddToHand<ExecutionCommanderScript>());
        await ScriptSystem.UnlockOrUpgradeAsync<ExecutionCommanderScriptRecord>(context,
            firstUpgraded ? upgraded : normal, 1);
        var record = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ExecutionCommanderScriptRecord>().Single();
        int initial = firstUpgraded ? 1 : 0;
        Assert.Equal(initial, record.CurrentUpgradeLevel);
        int expirations = 0;
        void Removed(PowerModel power)
        {
            if (power is not ScriptExecutionCommanderPower) return;
            Assert.Equal(initial + (expirations == 0 ? 0 : 2), record.CurrentUpgradeLevel);
            Assert.Equal(record.StoredAmount, power.Amount);
            expirations++;
        }
        Player.Creature.PowerRemoved += Removed;
        try
        {
            await ScriptSystem.UnlockOrUpgradeAsync<ExecutionCommanderScriptRecord>(context, upgraded, 1);
            Assert.Equal(initial + 2, record.CurrentUpgradeLevel);
            await ScriptSystem.UnlockOrUpgradeAsync<ExecutionCommanderScriptRecord>(context, normal, 1);
            Assert.Equal(initial + 3, record.CurrentUpgradeLevel);
        }
        finally { Player.Creature.PowerRemoved -= Removed; }
        Assert.Equal(2, expirations);
        Assert.Equal(2, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task General_expiration_uses_old_cards_and_upgrade_before_merging_new_records()
    {
        var context = new BlockingPlayerChoiceContext();
        var normal = await AddToHand<PartTimeGeneralScript>();
        var upgraded = Upgrade(await AddToHand<PartTimeGeneralScript>());
        await PlayerCmd.SetEnergy(1, Player);
        await Play(normal, EnemyAt(0));
        var record = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<PartTimeGeneralScriptRecord>().Single();
        var oldIds = record.RecordedCards.Select(card => CardModel.FromSerializable(card).Id).ToArray();
        Assert.NotEmpty(oldIds);
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Null(Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
            Assert.False(record.SourceUpgraded);
            Assert.False(record.IsUpgraded);
            Assert.Equal(oldIds.Length, record.RecordedCards.Count);
            Assert.Equal(oldIds, options.Select(card => card.Id));
            return [options[0]];
        });
        await PlayerCmd.SetEnergy(1, Player);
        using (UseCardSelector(selector)) await Play(upgraded, EnemyAt(0));
        AssertAllSelectionsConsumed(selector);
        Assert.True(record.SourceUpgraded);
        Assert.Equal(1, record.CurrentUpgradeLevel);
        Assert.True(record.RecordedCards.Count > oldIds.Length);
        Assert.NotNull(Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
    }

    [Fact]
    public async Task Played_effect_and_selection_precede_expiration_and_new_activation()
    {
        var context = new BlockingPlayerChoiceContext();
        await ApplyPower<GuanDiFormPower>(Player.Creature, 1);
        await ScriptSystem.UnlockOrUpgradeAsync<GruntScriptRecord>(context, await AddToHand<GruntScript>(), 1);
        await ScriptSystem.UnlockOrUpgradeAsync<ExecutionCommanderScriptRecord>(context, await AddToHand<ExecutionCommanderScript>(), 1);
        var outgoing = Player.Creature.GetPower<ScriptGruntPower>();
        var record = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<GruntScriptRecord>().Single();
        var servant = await AddToHand<ServantScript>();
        decimal block = Player.Creature.Block;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.True(Player.Creature.Block > block);
            Assert.Same(outgoing, Player.Creature.GetPower<ScriptGruntPower>());
            Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
            Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
            return [record];
        });
        using (UseCardSelector(selector)) await Play(servant);
        AssertAllSelectionsConsumed(selector);
        Assert.Null(Player.Creature.GetPower<ScriptGruntPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Manual_reselection_expires_all_old_scripts_before_any_activation_without_upgrading()
    {
        var context = new BlockingPlayerChoiceContext();
        await ApplyPower<GuanDiFormPower>(Player.Creature, 1);
        await ScriptSystem.UnlockOrUpgradeAsync<ExecutionCommanderScriptRecord>(context, await AddToHand<ExecutionCommanderScript>(), 1);
        await ScriptSystem.UnlockOrUpgradeAsync<GruntScriptRecord>(context, await AddToHand<GruntScript>(), 1);
        var records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
        var events = new List<string>();
        void Applied(PowerModel power) { if (power is ScriptPowerTemplate) events.Add("activate"); }
        void Removed(PowerModel power) { if (power is ScriptPowerTemplate) events.Add("expire"); }
        Player.Creature.PowerApplied += Applied;
        Player.Creature.PowerRemoved += Removed;
        try
        {
            var selector = new ScriptedCardSelector().ThenSelect(records);
            using (UseCardSelector(selector))
                Assert.True(await ScriptSystem.ChooseActiveScriptsAsync(context, Player, false, true));
            AssertAllSelectionsConsumed(selector);
        }
        finally
        {
            Player.Creature.PowerApplied -= Applied;
            Player.Creature.PowerRemoved -= Removed;
        }
        Assert.Equal(new[] { "expire", "expire", "activate", "activate" }, events);
        Assert.All(records, record => Assert.Equal(0, record.CurrentUpgradeLevel));
    }
}
