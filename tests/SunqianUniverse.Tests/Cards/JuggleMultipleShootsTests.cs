using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class JuggleMultipleShootsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("juggle-multiple-shoots");

    [Fact]
    public async Task Choice_builder_produces_three_distinct_other_character_powers_within_budget()
    {
        var card = Upgrade(await AddToHand<JuggleMultipleShoots>());
        MethodInfo builder = typeof(JuggleMultipleShoots).GetMethod(
            "BuildPowerChoices",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var choices = Assert.IsType<List<CardModel>>(builder.Invoke(card, [2]));

        Assert.Equal(3, choices.Count);
        Assert.Equal(choices.Count, choices.Select(choice => choice.Id).Distinct().Count());
        Assert.All(choices, choice =>
        {
            Assert.Equal(CardType.Power, choice.Type);
            Assert.InRange(choice.EnergyCost.Canonical, 0, 2);
        });
    }
}
