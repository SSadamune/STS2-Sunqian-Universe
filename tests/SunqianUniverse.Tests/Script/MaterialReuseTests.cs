using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class MaterialReuseTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("material-reuse");

    [Theory]
    [InlineData(typeof(ExecutionCommanderScript), typeof(ExecutionCommanderScriptRecord), 50, 25)]
    [InlineData(typeof(HeadsmanScript), typeof(HeadsmanScriptRecord), 3, 1)]
    [InlineData(typeof(BowangParadoxScript), typeof(BowangParadoxAdvanceScriptRecord), 7, 2)]
    [InlineData(typeof(BowangParadoxScript), typeof(BowangParadoxArsonScriptRecord), 7, 2)]
    public async Task Replays_upgrade_records_and_restore_their_values(Type sourceType, Type recordType, int initial, int step)
    {
        await PlayerCmd.SetEnergy(20, Player);
        for (int play = 0; play < 3; play++)
        {
            var source = (ScriptCardTemplate)await AddToHand(ModelDb.GetId(sourceType));
            if (play != 1) Upgrade(source);
            var selector = new ScriptedCardSelector();
            if (source is BowangParadoxScript) selector.ThenSelectWhere(card => card.GetType() == recordType);
            using (UseCardSelector(selector))
            {
                if (source is HeadsmanScript) await Play(source, EnemyAt(0));
                else await Play(source);
            }
            AssertAllSelectionsConsumed(selector);
            int level = play == 0 ? 1 : play == 1 ? 2 : 4;
            var records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
            var record = Assert.Single(records, card => card.GetType() == recordType);
            Assert.Equal(level, record.CurrentUpgradeLevel);
            Assert.Equal(initial + level * step, record.StoredAmount);
            Assert.Contains(SquKeywords.MaterialReuse, record.Keywords);
            var active = Assert.Single(Player.Creature.Powers.OfType<Squ.Powers.ScriptPowerTemplate>());
            Assert.Equal(record.PowerType, active.GetType());
            Assert.Equal(record.StoredAmount, active.Amount);
            if (source is BowangParadoxScript)
                Assert.All(records, branch => { Assert.Equal(level, branch.CurrentUpgradeLevel); Assert.Equal(initial + level * step, branch.StoredAmount); });
            var restored = Assert.IsAssignableFrom<ScriptRecordCardTemplate>(CardModel.FromSerializable(record.ToSerializable()));
            Assert.Equal(record.Title, restored.Title);
            Assert.Equal(record.StoredAmount, restored.StoredAmount);
        }
    }
}
