using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class SerpentSpearTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("serpent-spear");
    private static BlockingPlayerChoiceContext Choice() => new();
    private async Task<Creature> Third(int hp = 20) =>
        (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, hp, null)).Creature!;
    private async Task<(Creature Pet, MakeHolesPower Skill, CardModel Card)> Setup()
    {
        var pet = await Third();
        await CompanionSkills.UnlockForCompanion<MakeHolesPower>(Choice(), Player, CompanionSummons.ThirdBrotherId);
        return (pet, pet.GetPower<MakeHolesPower>()!, await AddToHand<SerpentSpear>());
    }
    private AttackCommand Attack(Creature pet, CardModel card, int amount = 10, bool all = false)
    {
        var play = new CardPlay { Card = card, Player = Player, Target = EnemyAt(0), ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
        var attack = DamageCmd.Attack(amount).FromCompanion(pet, card, play);
        return all ? attack.TargetingAllOpponents(Combat) : attack.Targeting(EnemyAt(0));
    }

    [Theory]
    [InlineData(false, 11, 2)]
    [InlineData(true, 15, 3)]
    public async Task First_spear_attacks_one_random_enemy_then_gives_third_brother_strength_and_skill(bool upgraded, int damage, int strength)
    {
        var card = await AddToHand<SerpentSpear>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardType.Attack, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(2, card.EnergyCost.GetResolved());
        Assert.False(card.CanPlay(out _, out _));
        var pet = await Third();
        await PowerCmd.Apply<StrengthPower>(Choice(), Player.Creature, 30, Player.Creature, null);
        int[] hp = Enumerable.Range(0, 4).Select(i => EnemyAt(i).CurrentHp).ToArray();
        await Play(card);
        int[] lost = Enumerable.Range(0, 4).Select(i => hp[i] - EnemyAt(i).CurrentHp).ToArray();
        Assert.Single(lost.Where(value => value == damage));
        Assert.Equal(damage, lost.Sum());
        Assert.Equal(20, pet.MaxHp);
        Assert.Equal(strength, pet.GetPower<StrengthPower>()!.Amount);
        Assert.True(pet.GetPower<MakeHolesPower>()!.IsActive);
        Assert.False(Player.Creature.HasPower<MakeHolesPower>());
        Assert.Contains(CompanionSkillCollection.Pile.GetPile(Player).Cards.OfType<CompanionSkillRecord>(), record =>
            record.CompanionId == CompanionSummons.ThirdBrotherId && record.Skill == "MakeHoles");
        Assert.Equal("你打出的[gold]三弟[/gold]攻击牌会附加随机效果。", pet.GetPower<MakeHolesPower>()!.Description.GetFormattedText());
    }

    [Fact]
    public async Task Spread_includes_the_original_target_and_two_distinct_random_enemies()
    {
        var (pet, skill, card) = await Setup();
        var attack = Attack(pet, card);
        await skill.Prepare(attack, MakeHolesPower.Effect.Spread);
        await attack.Execute(Choice());
        var results = attack.Results.SelectMany(hit => hit).ToArray();
        Assert.Equal(3, results.Length);
        Assert.Equal(3, results.Select(result => result.Receiver).Distinct().Count());
        Assert.Contains(results, result => result.Receiver == EnemyAt(0));
        Assert.All(results, result => Assert.Equal(10, result.TotalDamage));
    }

    [Fact]
    public async Task Spread_is_excluded_for_all_enemy_attacks_and_when_only_one_enemy_remains()
    {
        var (pet, skill, card) = await Setup();
        Assert.DoesNotContain(MakeHolesPower.Effect.Spread, skill.GetEligibleEffects(Attack(pet, card, all: true)));
        for (int i = 3; i >= 1; i--) await CreatureCmd.Kill(EnemyAt(i), force: true);
        Assert.DoesNotContain(MakeHolesPower.Effect.Spread, skill.GetEligibleEffects(Attack(pet, card)));
    }

    [Theory]
    [InlineData(1, 21)]
    [InlineData(2, 42)]
    public async Task Follow_up_uses_half_the_original_damage_on_the_same_target_without_recursive_rolls(int hits, int expected)
    {
        var (pet, skill, card) = await Setup();
        await PowerCmd.Apply<StrengthPower>(Choice(), pet, 4, Player.Creature, null);
        int hp = EnemyAt(0).CurrentHp;
        int otherHp = EnemyAt(1).CurrentHp;
        var attack = Attack(pet, card).WithHitCount(hits);
        await skill.Prepare(attack, MakeHolesPower.Effect.FollowUp);
        await attack.Execute(Choice());
        Assert.Equal(hp - expected, EnemyAt(0).CurrentHp);
        Assert.Equal(otherHp, EnemyAt(1).CurrentHp);
        Assert.False(EnemyAt(0).HasPower<BurningPower>());
        Assert.False(EnemyAt(0).HasPower<VulnerablePower>());
        Assert.False(EnemyAt(0).HasPower<WeakPower>());
    }

    [Fact]
    public async Task Burning_is_applied_after_every_hit_to_each_damaged_enemy()
    {
        var (pet, skill, card) = await Setup();
        var attack = Attack(pet, card, all: true).WithHitCount(3);
        await skill.Prepare(attack, MakeHolesPower.Effect.Burn);
        await attack.Execute(Choice());
        for (int i = 0; i < 4; i++) Assert.Equal(12, EnemyAt(i).GetPower<BurningPower>()!.Amount);
    }

    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 10)]
    public async Task Vulnerable_is_applied_before_damage_and_weak_after_damage(bool weak, int expected)
    {
        var (pet, skill, card) = await Setup();
        var attack = Attack(pet, card);
        int hp = EnemyAt(0).CurrentHp;
        await skill.Prepare(attack, weak ? MakeHolesPower.Effect.Weak : MakeHolesPower.Effect.Vulnerable);
        if (weak) Assert.False(EnemyAt(0).HasPower<WeakPower>());
        else Assert.True(EnemyAt(0).HasPower<VulnerablePower>());
        await attack.Execute(Choice());
        Assert.Equal(hp - expected, EnemyAt(0).CurrentHp);
        if (weak) Assert.Equal(1, EnemyAt(0).GetPower<WeakPower>()!.Amount);
        else Assert.Equal(1, EnemyAt(0).GetPower<VulnerablePower>()!.Amount);
    }

    [Fact]
    public async Task Both_debuff_outcomes_require_a_target_without_weak_even_if_vulnerable_is_already_present()
    {
        var (pet, skill, card) = await Setup();
        await PowerCmd.Apply<VulnerablePower>(Choice(), EnemyAt(0), 2, Player.Creature, null);
        Assert.Contains(MakeHolesPower.Effect.Vulnerable, skill.GetEligibleEffects(Attack(pet, card)));
        await PowerCmd.Apply<WeakPower>(Choice(), EnemyAt(0), 1, Player.Creature, null);
        Assert.DoesNotContain(MakeHolesPower.Effect.Vulnerable, skill.GetEligibleEffects(Attack(pet, card)));
        Assert.DoesNotContain(MakeHolesPower.Effect.Weak, skill.GetEligibleEffects(Attack(pet, card)));
        Assert.Contains(MakeHolesPower.Effect.Vulnerable, skill.GetEligibleEffects(Attack(pet, card, all: true)));
        for (int i = 1; i < 4; i++) await PowerCmd.Apply<WeakPower>(Choice(), EnemyAt(i), 1, Player.Creature, null);
        Assert.DoesNotContain(MakeHolesPower.Effect.Weak, skill.GetEligibleEffects(Attack(pet, card, all: true)));
        Assert.DoesNotContain(MakeHolesPower.Effect.Vulnerable, skill.GetEligibleEffects(Attack(pet, card, all: true)));
    }

    [Fact]
    public async Task Real_third_brother_attack_rolls_an_effect_and_second_brother_attack_does_not()
    {
        var (pet, skill, card) = await Setup();
        int[] hp = Enumerable.Range(0, 4).Select(i => EnemyAt(i).CurrentHp).ToArray();
        await Play(await AddToHand<NominalBrothers>(), EnemyAt(0));
        bool additionalEffect = Enumerable.Range(0, 4).Any(i => EnemyAt(i).HasPower<BurningPower>() ||
            EnemyAt(i).HasPower<VulnerablePower>() || EnemyAt(i).HasPower<WeakPower>() ||
            hp[i] - EnemyAt(i).CurrentHp != (i == 0 ? 13 : 0));
        Assert.True(additionalEffect);
        await CreatureCmd.Kill(pet, force: true);
        var second = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        var source = await AddToHand<GreenDragonCrescentBlade>();
        var attack = Attack(second, source);
        await skill.BeforeAttack(attack);
        await attack.Execute(Choice());
        Assert.Single(attack.Results.SelectMany(hit => hit));
    }

    [Fact]
    public async Task Inactive_skill_adds_no_effect_and_revival_preserves_the_unlock()
    {
        var pet = await Third(8);
        await CompanionSkills.UnlockForCompanion<HardToRestrainPower>(Choice(), Player, CompanionSummons.ThirdBrotherId);
        await CompanionSkills.UnlockForCompanion<MakeHolesPower>(Choice(), Player, CompanionSummons.ThirdBrotherId);
        Assert.False(pet.GetPower<MakeHolesPower>()!.IsActive);
        var source = await AddToHand<SerpentSpear>();
        var attack = Attack(pet, source);
        int hp = EnemyAt(0).CurrentHp;
        await attack.Execute(Choice());
        Assert.Equal(hp - 10, EnemyAt(0).CurrentHp);
        Assert.Single(attack.Results.SelectMany(hit => hit));
        Assert.False(EnemyAt(0).HasPower<BurningPower>());
        Assert.False(EnemyAt(0).HasPower<VulnerablePower>());
        Assert.False(EnemyAt(0).HasPower<WeakPower>());
        await CreatureCmd.Kill(pet, force: true);
        await Third(20);
        Assert.True(pet.GetPower<MakeHolesPower>()!.IsActive);
        Assert.Single(pet.Powers.OfType<MakeHolesPower>());
    }
}
