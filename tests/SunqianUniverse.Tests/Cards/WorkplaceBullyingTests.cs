using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class WorkplaceBullyingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("workplace-bullying");
    private static BlockingPlayerChoiceContext Choice() => new();

    [Theory]
    [InlineData(false, 5)]
    [InlineData(true, 7)]
    public async Task Summons_both_brothers_and_each_attacks_the_selected_enemy(bool upgraded, int summon)
    {
        var card = await AddToHand<WorkplaceBullying>();
        if (upgraded) Upgrade(card);
        Assert.Equal(2, card.EnergyCost.GetResolved());
        Assert.Equal(CardType.Attack, card.Type);
        Assert.Equal(CardRarity.Common, card.Rarity);
        Assert.Equal(TargetType.AnyEnemy, card.TargetType);
        int hp = EnemyAt(0).CurrentHp;
        int otherHp = EnemyAt(1).CurrentHp;
        await Play(card, EnemyAt(0));
        foreach (var id in new[] { CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId })
        {
            var pet = CompanionSummons.GetCompanion(Player, id)!;
            Assert.Equal(summon, pet.CurrentHp);
            Assert.Equal(summon, pet.MaxHp);
            Assert.False(pet.HasPower<SharedLifePower>());
        }
        Assert.Equal(hp - summon * 2, EnemyAt(0).CurrentHp);
        Assert.Equal(otherHp, EnemyAt(1).CurrentHp);
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        Assert.Contains("二者分别造成等于各自生命值的伤害。", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Existing_brothers_use_their_own_current_hp_after_summoning_and_their_own_strength()
    {
        var second = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        var third = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, 30, null)).Creature!;
        await CreatureCmd.SetCurrentHp(second, 3);
        await CreatureCmd.SetCurrentHp(third, 7);
        await PowerCmd.Apply<StrengthPower>(Choice(), second, 2, Player.Creature, null);
        await PowerCmd.Apply<StrengthPower>(Choice(), third, 4, Player.Creature, null);
        await PowerCmd.Apply<StrengthPower>(Choice(), Player.Creature, 50, Player.Creature, null);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<WorkplaceBullying>(), EnemyAt(0));
        Assert.Equal(8, second.CurrentHp);
        Assert.Equal(12, third.CurrentHp);
        Assert.Equal(25, second.MaxHp);
        Assert.Equal(35, third.MaxHp);
        Assert.Equal(hp - (8 + 2) - (12 + 4), EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Dead_brother_is_revived_before_attacking_and_retains_unlocked_skills()
    {
        var second = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        await CompanionSkills.UnlockForCompanion<SpareTheYoungAndOldPower>(Choice(), Player, CompanionSummons.SecondBrotherId);
        await CreatureCmd.Kill(second, force: true);
        await CreatureCmd.SetMaxAndCurrentHp(EnemyAt(0), 100);
        await CreatureCmd.SetCurrentHp(EnemyAt(0), 50);
        await Play(await AddToHand<WorkplaceBullying>(), EnemyAt(0));
        Assert.True(second.IsAlive);
        Assert.Equal(5, second.CurrentHp);
        Assert.Equal(35, EnemyAt(0).CurrentHp); // Second Brother doubles his five; Third Brother deals five.
        Assert.Single(second.Powers.OfType<SpareTheYoungAndOldPower>());
    }

    [Fact]
    public async Task Both_are_summoned_even_if_the_first_attack_kills_the_target_and_second_does_not_retarget()
    {
        await CreatureCmd.SetCurrentHp(EnemyAt(0), 1);
        var target = EnemyAt(0);
        var other = EnemyAt(1);
        int hp = other.CurrentHp;
        await Play(await AddToHand<WorkplaceBullying>(), target);
        Assert.False(target.IsAlive);
        Assert.Equal(hp, other.CurrentHp);
        Assert.Equal(5, CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!.CurrentHp);
        Assert.Equal(5, CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!.CurrentHp);
    }

    [Fact]
    public async Task Third_brothers_attack_triggers_make_holes()
    {
        await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, 20, null);
        await CompanionSkills.UnlockForCompanion<MakeHolesPower>(Choice(), Player, CompanionSummons.ThirdBrotherId);
        int hp = EnemyAt(0).CurrentHp;
        int otherHp = EnemyAt(1).CurrentHp;
        await Play(await AddToHand<WorkplaceBullying>(), EnemyAt(0));
        Assert.True(EnemyAt(0).CurrentHp != hp - 30 || EnemyAt(1).CurrentHp != otherHp ||
            EnemyAt(0).HasPower<BurningPower>() || EnemyAt(0).HasPower<WeakPower>() || EnemyAt(0).HasPower<VulnerablePower>());
    }
}
