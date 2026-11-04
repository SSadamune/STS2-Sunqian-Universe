using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ParameterizedKeywordRowsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("parameterized-keyword-rows");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Self_harm_and_retain_merge_without_absorbing_companion_attack_rule(bool upgraded)
    {
        await CompanionSummons.SummonSpecific(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), Player, CompanionSummons.SecondBrotherId, 8, null);
        var card = await AddToHand<GreenDragonCrescentBlade>();
        if (upgraded) Upgrade(card);
        SelfHarmSystem.Grant(card, 3);
        CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        Assert.Contains("[gold]保留[/gold]", lines[0]);
        Assert.Contains("[gold]自残[/gold]", lines[0]);
        Assert.DoesNotContain("召唤", lines[0]);
        Assert.DoesNotContain("造成", lines[0]);
        Assert.Contains("造成", lines[1]);
        await Play(card);
        Assert.DoesNotContain("自残", card.GetDescriptionForPile(PileType.Discard));
        Assert.Contains("[gold]二弟[/gold]", card.GetDescriptionForPile(PileType.Discard));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Granted_self_harm_and_native_summon_use_the_same_row(bool upgraded)
    {
        var card = await AddToHand<Bodyguard>();
        if (upgraded) Upgrade(card);
        CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        SelfHarmSystem.Grant(card, 3);
        var text = card.GetDescriptionForPile(PileType.Hand);
        Assert.DoesNotContain('\n', text);
        Assert.Contains("[gold]保留[/gold]", text);
        Assert.Contains("[gold]自残[/gold]", text);
        Assert.Contains("[gold]召唤[/gold]", text);
        await Play(card);
        Assert.DoesNotContain("自残", card.GetDescriptionForPile(PileType.Discard));
    }

    [Fact]
    public async Task Parameterized_clause_after_a_regular_rule_does_not_merge_across_it()
    {
        var card = await AddToHand<UnstableFactor>();
        CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        Assert.Contains("[gold]保留[/gold]", lines[0]);
        Assert.DoesNotContain("召唤", lines[0]);
        Assert.Contains("每回合开始", lines[1]);
        Assert.Contains("[gold]召唤[/gold]", lines[1]);
    }
}
