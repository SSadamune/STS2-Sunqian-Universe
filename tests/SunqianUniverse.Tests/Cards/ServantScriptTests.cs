using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ServantScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-servant-script");
    }

    [Fact]
    public async Task ServantScript_grants_block_and_becomes_active_script()
    {
        var card = await AddToHand<ServantScript>();

        await Play(card);

        Assert.Equal(ServantScript.BaseBlock, Player.Creature.Block);
        AssertOnlyScript<ScriptServantPower>();
    }
}
