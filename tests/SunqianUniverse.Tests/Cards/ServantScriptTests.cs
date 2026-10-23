using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
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

        await PlayAndActivateScript(card);

        Assert.Equal(ServantScript.BaseBlock, Player.Creature.Block);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task ThrowHimOut_grants_block_and_vigor_then_exhausts()
    {
        var card = await AddToHand<ThrowHimOut>();

        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        await Play(card);

        Assert.Equal(ThrowHimOut.BaseBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            ThrowHimOut.BaseVigor);
        AssertInPile(card, PileType.Exhaust);
    }

    [Fact]
    public async Task Upgraded_ThrowHimOut_grants_five_block_and_three_vigor()
    {
        var card = Upgrade(await AddToHand<ThrowHimOut>());

        await Play(card);

        Assert.Equal(ThrowHimOut.UpgradedBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            ThrowHimOut.UpgradedVigor);
        AssertInPile(card, PileType.Exhaust);
    }
}
