using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using STS2RitsuLib.Scaffolding.Characters;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Character;

public sealed class SunqianCardPoolTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("colorful-philosophers-card-pool");

    [Fact]
    public void Card_pool_is_registered_for_colorful_philosophers()
    {
        var cardPool = ModelDb.CardPool<SunqianCardPool>();

        Assert.IsAssignableFrom<IModColorfulPhilosophersCardPool>(cardPool);
        Assert.Equal("sunqian", cardPool.EnergyColorName);
    }

    [Fact]
    public void Starting_deck_contains_eleven_cards_with_two_basic_scripts()
    {
        var deck = ModelDb.Character<SunqianCharacter>().StartingDeck;

        Assert.Equal(11, deck.Count());
        Assert.Equal(4, deck.OfType<StrikeLongtao>().Count());
        Assert.Equal(4, deck.OfType<DefendLongtao>().Count());
        Assert.Single(deck.OfType<GruntScript>());
        Assert.Single(deck.OfType<SunqianScript>());
        Assert.Single(deck.OfType<WrapUp>());
    }

    [Fact]
    public async Task Other_character_can_receive_the_grape_purple_option()
    {
        Assert.IsType<Ironclad>(Player.Character);

        var evt = (ColorfulPhilosophers)ModelDb.Event<ColorfulPhilosophers>().ToMutable();
        await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);

        const string sunqianOptionKey =
            "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.SUNQIAN";
        var option = Assert.Single(
            evt.CurrentOptions,
            candidate => candidate.TextKey == sunqianOptionKey);
        Assert.Equal(sunqianOptionKey + ".title", option.Title.LocEntryKey);
        Assert.Equal(sunqianOptionKey + ".description", option.Description.LocEntryKey);
        Assert.True(option.Title.Exists());
        Assert.True(option.Description.Exists());
    }
}
