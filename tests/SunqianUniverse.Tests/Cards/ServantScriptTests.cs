using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
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
        Assert.Contains(PileType.Hand.GetPile(Player).Cards, card => card is ThrowHimOut);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task ThrowHimOut_deals_damage_and_gains_equal_block_then_exhausts()
    {
        var card = await AddToHand<ThrowHimOut>();
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;

        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.Equal(CardType.Attack, card.Type);
        await Play(card, enemy);

        Assert.Equal(hp - 4, enemy.CurrentHp);
        Assert.Equal(4, Player.Creature.Block);
        AssertInPile(card, PileType.Exhaust);
    }

    [Fact]
    public async Task Upgraded_ThrowHimOut_counts_fully_blocked_damage_for_its_block()
    {
        var card = Upgrade(await AddToHand<ThrowHimOut>());
        var enemy = EnemyAt(0);
        await CreatureCmd.GainBlock(enemy, 100, ValueProp.Unpowered, null);
        int hp = enemy.CurrentHp;

        await Play(card, enemy);

        Assert.Equal(hp, enemy.CurrentHp);
        Assert.Equal(6, Player.Creature.Block);
        AssertInPile(card, PileType.Exhaust);
    }
}
