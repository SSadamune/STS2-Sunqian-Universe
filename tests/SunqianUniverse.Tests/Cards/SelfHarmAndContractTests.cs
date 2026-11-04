using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Characters;
using Squ.Combat;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class SelfHarmAndContractTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("self-harm-regression");

    [Fact]
    public async Task Granted_self_harm_is_green_free_and_paid_once_without_shared_life_credit()
    {
        var card = await AddToHand<Bodyguard>();
        SelfHarmSystem.Grant(card, 8);
        await PlayerCmd.SetEnergy(0, Player);
        int hp = Player.Creature.CurrentHp;
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Equal(CardCostColor.Decreased, CardCostHelper.GetEnergyCostColor(card, Combat));
        await Play(card);
        Assert.Equal(hp - 8, Player.Creature.CurrentHp);
        Assert.False(SelfHarmSystem.HasSelfHarm(card));
        Assert.DoesNotContain("自残", card.GetDescriptionForPile(PileType.Discard));
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player));
        await CardPileCmd.Add(card, PileType.Hand);
        await PlayerCmd.SetEnergy(3, Player);
        await Play(card);
        Assert.Equal(hp - 8, Player.Creature.CurrentHp);
        Assert.Equal(2, Player.PlayerCombatState!.Energy);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(7)]
    public async Task At_or_below_threshold_energy_is_paid_and_self_harm_is_preserved(int hp)
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, hp);
        var card = await AddToHand<Bodyguard>();
        SelfHarmSystem.Grant(card, 8);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        int energy = Player.PlayerCombatState!.Energy;
        await Play(card);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(energy - 1, Player.PlayerCombatState.Energy);
        Assert.True(SelfHarmSystem.HasSelfHarm(card));
    }

    [Fact]
    public async Task Hp_changes_toggle_the_free_cost_and_combat_cleanup_removes_granted_self_harm()
    {
        var card = await AddToHand<Bodyguard>();
        SelfHarmSystem.Grant(card, 8);
        await CreatureCmd.SetCurrentHp(Player.Creature, 8);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        await CreatureCmd.SetCurrentHp(Player.Creature, 9);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        SelfHarmSystem.ClearCombat(Combat);
        Assert.False(SelfHarmSystem.HasSelfHarm(card));
        Assert.Equal(1, card.EnergyCost.GetResolved());
    }
}
