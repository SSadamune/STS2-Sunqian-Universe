using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LeadCiviliansAcrossTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("lead-civilians-across");

    [Theory]
    [InlineData(false, 12)]
    [InlineData(true, 16)]
    public async Task First_play_pays_six_hp_and_grants_upgraded_temporary_health(bool upgraded, int amount)
    {
        int hp = Player.Creature.CurrentHp;
        var card = await AddToHand<LeadCiviliansAcross>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(2, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(6m, card.SelfHarmAmount);
        await PlayerCmd.SetEnergy(0, Player);
        await Play(card);
        Assert.Equal(hp - 6 + amount, Player.Creature.CurrentHp);
        Assert.Equal(amount, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
        AssertInPile(card, PileType.Discard);
    }

    [Fact]
    public async Task Subsequent_play_costs_two_energy_and_does_not_pay_hp_again()
    {
        int hp = Player.Creature.CurrentHp;
        var card = await AddToHand<LeadCiviliansAcross>();
        await Play(card);
        await CardPileCmd.Add(card, PileType.Hand);
        int energy = Player.PlayerCombatState!.Energy;
        Assert.Equal(2, card.EnergyCost.GetResolved());
        await Play(card);
        Assert.Equal(energy - 2, Player.PlayerCombatState.Energy);
        Assert.Equal(hp - 6 + 24, Player.Creature.CurrentHp);
        Assert.Equal(24, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Fact]
    public async Task At_six_hp_uses_energy_without_self_harm()
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, 6);
        var card = await AddToHand<LeadCiviliansAcross>();
        Assert.Equal(2, card.EnergyCost.GetResolved());
        await Play(card);
        Assert.Equal(18, Player.Creature.CurrentHp);
        Assert.Equal(12, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Fact]
    public async Task Gained_temporary_health_loses_ten_on_each_own_turn_without_harming_real_hp()
    {
        int hp = Player.Creature.CurrentHp;
        await Play(await AddToHand<LeadCiviliansAcross>());
        var power = Player.Creature.GetPower<TemporaryHealthPower>()!;
        await power.BeforeSideTurnStart(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            Player.Creature.Side, [Player.Creature], Combat);
        Assert.Equal(2, power.Amount);
        Assert.Equal(hp - 6 + 2, Player.Creature.CurrentHp);
        await power.BeforeSideTurnStart(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            Player.Creature.Side, [Player.Creature], Combat);
        Assert.Null(Player.Creature.GetPower<TemporaryHealthPower>());
        Assert.Equal(hp - 6, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Registered_in_prisoner_pool_and_hover_explains_self_harm_six()
    {
        var card = await AddToHand<LeadCiviliansAcross>();
        Assert.Contains(ModelDb.CardPool<LiubeiCardPool>()
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint),
            entry => entry is LeadCiviliansAcross);
        Assert.DoesNotContain(ModelDb.CardPool<SunqianCardPool>()
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint),
            entry => entry is LeadCiviliansAcross);
        Assert.Contains(card.HoverTips.OfType<HoverTip>(), tip => tip.Title?.EndsWith("6") == true);
    }
}
