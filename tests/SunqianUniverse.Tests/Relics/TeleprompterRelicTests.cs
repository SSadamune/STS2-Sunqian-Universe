using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Relics;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class TeleprompterRelicTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("teleprompter-collection-growth");

    [Fact]
    public async Task Every_three_collection_additions_draw_two_and_reordering_does_not_count()
    {
        var relic = (TeleprompterRelic)ModelDb.Relic<TeleprompterRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        var one = await AddToDrawPileTop<Wound>();
        var two = await AddToDrawPileTop<Wound>();
        var record = await AddCardToPile<ServantScriptRecord>(ScriptSystem.ScriptCollectionPile);
        Assert.Equal(1, relic.ScriptsAdded);
        await CardPileCmd.Add(record, ScriptSystem.ScriptCollectionPile);
        Assert.Equal(1, relic.ScriptsAdded);
        await CardPileCmd.Add(record, PileType.Discard);
        Assert.Equal(1, relic.ScriptsAdded);
        await CardPileCmd.Add(record, ScriptSystem.ScriptCollectionPile);
        Assert.Equal(2, relic.ScriptsAdded);
        AssertInPile(one, PileType.Draw);
        await AddCardToPile<GruntScriptRecord>(ScriptSystem.ScriptCollectionPile);
        Assert.Equal(0, relic.ScriptsAdded);
        AssertInPile(one, PileType.Hand);
        AssertInPile(two, PileType.Hand);
    }

    [Fact]
    public async Task Reinforcing_and_lifting_scripts_do_not_count_as_collection_additions()
    {
        var relic = (TeleprompterRelic)ModelDb.Relic<TeleprompterRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await Play(await AddToHand<ExecutionCommanderScript>());
        await Play(await AddToHand<ExecutionCommanderScript>());
        Assert.Equal(1, relic.ScriptsAdded);
        await ScriptSystem.InvalidateScriptsAsync(Player.Creature);
        Assert.Equal(1, relic.ScriptsAdded);
    }
}
