using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Runs;
using System.Reflection;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KnowWrongDenyWrongTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("know-wrong-deny-wrong");

    [Fact]
    public async Task Upgraded_card_deals_twice_the_current_run_reload_count()
    {
        const int reloads = 3;
        FieldInfo field = typeof(RunManager).GetField("_numReloads", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(RunManager.Instance, reloads);
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        var card = Upgrade(await AddToHand<KnowWrongDenyWrong>());

        await Play(card, enemy);

        Assert.Equal(hp - reloads * 2, enemy.CurrentHp);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.Contains(CardKeyword.Ethereal, card.Keywords);
    }
}
