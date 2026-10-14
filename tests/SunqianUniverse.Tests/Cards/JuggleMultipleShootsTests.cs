using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;
using Squ.Cards;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class JuggleMultipleShootsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("juggle-multiple-shoots");

    [Fact]
    public async Task Wrap_consumes_the_active_script_and_upgrades_and_discounts_the_selected_power()
    {
        await ApplyPower<ScriptOneManOneCityPower>(Player.Creature, 1);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            selected = options[0];
            return [selected];
        });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<JuggleMultipleShoots>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(selected);
        Assert.True(selected.IsUpgraded);
        Assert.Equal(Math.Max(0, selected.EnergyCost.Canonical - 1), selected.EnergyCost.GetResolved());
        Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
    }

    [Fact]
    public async Task Play_adds_the_selected_generated_power_to_hand()
    {
        CardModel? selected = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            selected = options[0];
            return [selected];
        });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<JuggleMultipleShoots>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(selected);
        AssertInPile(selected, PileType.Hand);
        Assert.Equal(Player, selected.Owner);
    }

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
