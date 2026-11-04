using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HarmonyLib;
using Squ;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GreenDragonCrescentBladeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("companion-skills");

    private async Task SummonSecond() => await CompanionSummons.SummonSpecific(
        new BlockingPlayerChoiceContext(), Player, CompanionSummons.SecondBrotherId, 8, null);

    [Theory]
    [InlineData(false, 9, 2)]
    [InlineData(true, 13, 3)]
    public async Task Blade_attacks_all_enemies_before_granting_strength_without_summoning(bool upgraded, int damage, int strength)
    {
        var card = await AddToHand<GreenDragonCrescentBlade>();
        if (upgraded) Upgrade(card);
        Assert.Equal(2, card.EnergyCost.GetResolved());
        Assert.False(card.CanPlay(out _, out _));
        HoverTip keyword = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(SquKeywords.CompanionSkill));
        Assert.Equal("召唤物技能", keyword.Title);
        Assert.Equal("召唤物只有第一个被解锁的技能生效，每拥有10点生命上限，可以多生效一个技能。", keyword.Description);
        Assert.Contains(card.HoverTips, tip => tip.Id == keyword.Id);
        HoverTip skill = Assert.IsType<HoverTip>(Assert.Single(card.HoverTips, tip => tip is HoverTip hover && hover.Title == "不斩老幼"));
        Assert.StartsWith("[gold]召唤物技能[/gold]：", skill.Description);
        Assert.Contains("对生命值大于25%且小于75%的敌人造成双倍伤害。", skill.Description);
        Assert.DoesNotContain("按解锁顺序", skill.Description);
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        await SummonSecond();
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Player.Creature, 20, Player.Creature, null);
        int[] hp = [EnemyAt(0).CurrentHp, EnemyAt(1).CurrentHp];
        await Play(card);
        Assert.Equal(hp[0] - damage, EnemyAt(0).CurrentHp);
        Assert.Equal(hp[1] - damage, EnemyAt(1).CurrentHp);
        Assert.Equal(8, pet.MaxHp);
        Assert.Equal(strength, pet.GetPower<StrengthPower>()!.Amount);
        Assert.Equal(20, Player.Creature.GetPower<StrengthPower>()!.Amount);
        Assert.DoesNotContain("召唤", card.GetDescriptionForPile(PileType.Hand));
        await CreatureCmd.Kill(pet, force: true);
        var other = await AddToHand<GreenDragonCrescentBlade>();
        Assert.False(other.CanPlay(out _, out _));
    }

    [Theory]
    [InlineData(false, false, 8, 4)]
    [InlineData(false, true, 8, 5)]
    [InlineData(true, false, 8, 5)]
    [InlineData(true, true, 8, 6)]
    public async Task Repeated_cards_stack_only_companion_strength(bool firstUpgraded,
        bool secondUpgraded, int hp, int strength)
    {
        await SummonSecond();
        await PlayerCmd.SetEnergy(4, Player);
        var first = await AddToHand<GreenDragonCrescentBlade>();
        if (firstUpgraded) Upgrade(first);
        Assert.Equal(CardType.Attack, first.Type);
        var description = first.GetDescriptionForPile(PileType.Hand);
        Assert.Equal(2, description.Split('\n').Length);
        Assert.Contains("[gold]二弟[/gold]获得", description);
        Assert.Contains("并解锁", description);
        Assert.Contains("解锁", first.GetDescriptionForPile(PileType.Hand));
        await Play(first);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        Assert.Equal(1, pet.GetPower<SpareTheYoungAndOldPower>()!.Amount);
        Assert.Equal(firstUpgraded ? 3m : 2m, pet.GetPower<StrengthPower>()!.Amount);
        var second = await AddToHand<GreenDragonCrescentBlade>();
        if (secondUpgraded) Upgrade(second);
        Assert.DoesNotContain("升级", second.GetDescriptionForPile(PileType.Hand));
        await Play(second);
        Assert.Equal(hp, pet.MaxHp);
        Assert.Equal(1, pet.GetPower<SpareTheYoungAndOldPower>()!.Amount);
        Assert.Equal((decimal)strength, pet.GetPower<StrengthPower>()!.Amount);
        Assert.False(Player.Creature.HasPower<StrengthPower>());
    }

    [Theory]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(74, 2)]
    [InlineData(75, 1)]
    public async Task Double_damage_uses_strict_health_boundaries(int hp, int multiplier)
    {
        await SummonSecond();
        await Play(await AddToHand<GreenDragonCrescentBlade>());
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.SetMaxAndCurrentHp(EnemyAt(0), 100);
        await CreatureCmd.SetCurrentHp(EnemyAt(0), hp);
        int before = EnemyAt(0).CurrentHp;
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), EnemyAt(0), 5, ValueProp.Move, pet);
        Assert.Equal(before - 7 * multiplier, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Revival_keeps_skill_but_does_not_restore_card_strength()
    {
        await SummonSecond();
        await Play(await AddToHand<GreenDragonCrescentBlade>());
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.Kill(pet, force: true);
        await CompanionSummons.SummonSpecific(new BlockingPlayerChoiceContext(), Player,
            CompanionSummons.SecondBrotherId, 3, null);
        Assert.Equal(1, pet.GetPower<SpareTheYoungAndOldPower>()!.Amount);
        Assert.Null(pet.GetPower<StrengthPower>());
        await CreatureCmd.GainMaxHp(pet, 10);
        Assert.Null(pet.GetPower<StrengthPower>());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    public void Skill_slots_require_ten_more_max_hp(int slot, int hp) =>
        Assert.Equal(hp, CompanionSkills.RequiredMaxHp(slot));

    [Fact]
    public async Task Later_skill_slots_activate_and_suspend_at_max_hp_thresholds()
    {
        await SummonSecond();
        await Play(await AddToHand<GreenDragonCrescentBlade>());
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        // Add independent skill instances to exercise future second/third skills.
        var second = (SpareTheYoungAndOldPower)ModelDb.Power<SpareTheYoungAndOldPower>().ToMutable();
        var third = (SpareTheYoungAndOldPower)ModelDb.Power<SpareTheYoungAndOldPower>().ToMutable();
        foreach (var skill in new[] { second, third })
        {
            Traverse.Create(skill).Field("_owner").SetValue(pet);
            skill.SetAmount(1);
            Traverse.Create(pet).Field<List<PowerModel>>("_powers").Value.Add(skill);
        }
        Assert.Equal(10, second.RequiredMaxHp);
        Assert.Equal(20, third.RequiredMaxHp);
        Assert.False(second.IsActive);
        Assert.False(third.IsActive);
        Assert.Equal(2, pet.GetPower<StrengthPower>()!.Amount);
        await CreatureCmd.SetMaxHp(pet, 10);
        Assert.True(second.IsActive);
        Assert.Equal(2, pet.GetPower<StrengthPower>()!.Amount);
        await CreatureCmd.SetMaxHp(pet, 20);
        Assert.True(third.IsActive);
        Assert.Equal(2, pet.GetPower<StrengthPower>()!.Amount);
        await CreatureCmd.SetMaxHp(pet, 9);
        Assert.False(second.IsActive);
        Assert.False(third.IsActive);
        Assert.Equal(2, pet.GetPower<StrengthPower>()!.Amount);
    }
    [Theory]
    [InlineData(false, 100, 15)]
    [InlineData(true, 100, 16)]
    [InlineData(false, 50, 30)]
    [InlineData(true, 50, 32)]
    public async Task Blade_strength_increases_nominal_brothers_damage(bool upgraded, int enemyHp, int expected)
    {
        await SummonSecond();
        var blade = await AddToHand<GreenDragonCrescentBlade>();
        if (upgraded) Upgrade(blade);
        await Play(blade);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        Assert.Equal(upgraded ? 3 : 2, pet.GetPower<StrengthPower>()!.Amount);
        Assert.False(Player.Creature.HasPower<StrengthPower>());
        // Player Strength must not affect an attack dealt by Second Brother.
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Player.Creature, 10, Player.Creature, null);
        await CreatureCmd.SetMaxAndCurrentHp(EnemyAt(0), 100);
        await CreatureCmd.SetCurrentHp(EnemyAt(0), enemyHp);
        await Play(await AddToHand<NominalBrothers>(), EnemyAt(0));
        Assert.Equal(enemyHp - expected, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Unlocking_skill_alone_grants_no_strength_and_never_upgrades()
    {
        var ctx = new BlockingPlayerChoiceContext();
        var pet = (await CompanionSummons.SummonSpecific(ctx, Player, CompanionSummons.SecondBrotherId, 8, null)).Creature!;
        var source = await AddToHand<GreenDragonCrescentBlade>();
        Upgrade(source);
        await CompanionSkills.Unlock<SpareTheYoungAndOldPower>(ctx, pet, source);
        await CompanionSkills.Unlock<SpareTheYoungAndOldPower>(ctx, pet, source);
        Assert.Equal(1, pet.GetPower<SpareTheYoungAndOldPower>()!.Amount);
        Assert.Single(pet.Powers.OfType<SpareTheYoungAndOldPower>());
        Assert.False(pet.HasPower<StrengthPower>());
    }

}
