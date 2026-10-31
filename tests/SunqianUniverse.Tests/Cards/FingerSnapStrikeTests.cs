using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class FingerSnapStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-finger-snap-strike");
    }

    [Fact]
    public async Task FingerSnapStrike_deals_its_base_damage()
    {
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        var card = await AddToHand<FingerSnapStrike>();
        Assert.DoesNotContain("[blue]", card.GetDescriptionForPile(MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand));

        await Play(card, enemy);

        Assert.Equal(hpBefore - FingerSnapStrike.BaseDamage, enemy.CurrentHp);
    }

    [Fact]
    public void Retain_keyword_shares_the_line_in_front_of_charge_text()
    {
        AssertRetainSharesChargeLine<FingerSnapStrike>("造成");
        AssertRetainSharesChargeLine<ThunderOnStillLake>("两次");
        AssertRetainSharesChargeLine<KeepCalm>("获得");
    }

    private static void AssertRetainSharesChargeLine<TCard>(string bodyMarker) where TCard : CardModel
    {
        string description = ModelDb.Card<TCard>().GetDescriptionForPile(PileType.Hand);
        string[] lines = description.Split('\n');
        int bodyLine = Array.FindIndex(lines, line => line.Contains(bodyMarker, StringComparison.Ordinal));
        int chargeLine = Array.FindIndex(lines, line => line.StartsWith("[gold]保留[/gold]。[gold]蓄能[/gold]", StringComparison.Ordinal));
        Assert.True(bodyLine >= 0 && chargeLine > bodyLine, description);
        Assert.DoesNotContain(
            lines[..chargeLine],
            line => line.StartsWith("[gold]保留[/gold]。", StringComparison.Ordinal));
    }
}
