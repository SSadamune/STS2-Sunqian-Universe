using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class IHaveAFamousQuoteTooTests : SunqianCombatTestSuite
{
    [Fact]
    public void Card_belongs_only_to_the_reincarnation_prisoner_pool()
    {
        var liubeiCards = ModelDb.CardPool<LiubeiCardPool>()
            .GetUnlockedCards(Player.UnlockState, CardMultiplayerConstraint.MultiplayerOnly);
        var sunqianCards = ModelDb.CardPool<SunqianCardPool>()
            .GetUnlockedCards(Player.UnlockState, CardMultiplayerConstraint.MultiplayerOnly);
        Assert.Contains(liubeiCards, card => card is IHaveAFamousQuoteToo);
        Assert.DoesNotContain(sunqianCards, card => card is IHaveAFamousQuoteToo);
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("i-have-a-famous-quote-too");

    [Theory]
    [InlineData(60, 20, 40)]
    [InlineData(61, 20, 40)]
    [InlineData(2, 1, 1)]
    public async Task Splits_current_hp_and_exhausts(int ownHp, int allyHp, int expected)
    {
        var ally = Players.Single(player => player != Player).Creature;
        await CreatureCmd.SetCurrentHp(Player.Creature, ownHp);
        await CreatureCmd.SetCurrentHp(ally, allyHp);
        var card = await AddToHand<IHaveAFamousQuoteToo>();

        await Play(card, ally);

        Assert.Equal(expected, Player.Creature.CurrentHp);
        Assert.Equal(expected, ally.CurrentHp);
        AssertInPile(card, PileType.Exhaust);
    }

    [Fact]
    public async Task Targeting_only_accepts_living_allies_with_strictly_less_hp()
    {
        var ally = Players.Single(player => player != Player).Creature;
        var card = await AddToHand<IHaveAFamousQuoteToo>();
        await CreatureCmd.SetCurrentHp(Player.Creature, 40);
        Assert.False(card.IsValidTarget(Player.Creature));
        Assert.False(card.IsValidTarget(EnemyAt(0)));
        await CreatureCmd.SetCurrentHp(ally, 40);
        Assert.False(card.IsValidTarget(ally));
        await CreatureCmd.SetCurrentHp(ally, 50);
        Assert.False(card.IsValidTarget(ally));
        await CreatureCmd.SetCurrentHp(ally, 20);
        Assert.True(card.IsValidTarget(ally));
    }

    [Fact]
    public async Task Upgrade_costs_zero_and_keeps_exhaust()
    {
        var card = await AddToHand<IHaveAFamousQuoteToo>();
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(CardMultiplayerConstraint.MultiplayerOnly, card.MultiplayerConstraint);
        Upgrade(card);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        var ally = Players.Single(player => player != Player).Creature;
        await CreatureCmd.SetCurrentHp(Player.Creature, 60);
        await CreatureCmd.SetCurrentHp(ally, 20);
        await Play(card, ally);
        Assert.Equal(40, ally.CurrentHp);
        AssertInPile(card, PileType.Exhaust);
    }

    [Fact]
    public async Task Shared_hp_is_capped_at_each_players_max_hp()
    {
        var ally = Players.Single(player => player != Player).Creature;
        await CreatureCmd.SetMaxHp(ally, 25);
        await CreatureCmd.SetCurrentHp(ally, 20);
        await CreatureCmd.SetCurrentHp(Player.Creature, 60);
        await Play(await AddToHand<IHaveAFamousQuoteToo>(), ally);
        Assert.Equal(40, Player.Creature.CurrentHp);
        Assert.Equal(25, ally.CurrentHp);
    }
}
