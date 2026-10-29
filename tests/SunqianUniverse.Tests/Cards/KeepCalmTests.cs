using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KeepCalmTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("keep-calm");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unblocked_damage_preserves_base_energy_and_only_converts_accumulated_energy(bool attack)
    {
        var card = Upgrade(await AddToHand<KeepCalm>());
        await EndTurn();
        int storedEnergy = card.DynamicVars.Energy.IntValue;
        var enemy = EnemyAt(0);

        if (attack)
            await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(null);
        else
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Player.Creature, 1, ValueProp.Unpowered, enemy);

        Assert.Equal(1, card.DynamicVars.Energy.IntValue);
        Assert.Equal(storedEnergy - 1, PileType.Hand.GetPile(Player).Cards.OfType<Anger>().Count());
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(null);
        Assert.Equal(1, card.DynamicVars.Energy.IntValue);
        Assert.Equal(storedEnergy - 1, PileType.Hand.GetPile(Player).Cards.OfType<Anger>().Count());
    }

    [Fact]
    public async Task Fully_blocked_damage_preserves_the_accumulated_energy()
    {
        var card = Upgrade(await AddToHand<KeepCalm>());
        await EndTurn();
        int energy = card.DynamicVars.Energy.IntValue;
        await CreatureCmd.GainBlock(Player.Creature, 10, ValueProp.Unpowered, null);
        await DamageCmd.Attack(1).FromMonster(EnemyAt(0).Monster!).WithNoAttackerAnim().Execute(null);
        Assert.Equal(energy, card.DynamicVars.Energy.IntValue);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<Anger>());
    }

    [Fact]
    public async Task Retaining_upgraded_card_adds_two_to_stored_energy_then_play_refunds_it()
    {
        var card = Upgrade(await AddToHand<KeepCalm>());
        await EndTurn();
        AssertInPile(card, PileType.Hand);
        Assert.Equal(KeepCalm.CanonicalEnergy + KeepCalm.UpgradedRetainBonus, card.DynamicVars.Energy.IntValue);

        int before = Player.PlayerCombatState!.Energy;
        await Play(card);
        Assert.Equal(before - 1 + KeepCalm.CanonicalEnergy + KeepCalm.UpgradedRetainBonus, Player.PlayerCombatState.Energy);
    }
}
