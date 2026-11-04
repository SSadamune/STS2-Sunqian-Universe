using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class UnstableFactorTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("unstable-factor");
    private static BlockingPlayerChoiceContext Choice() => new();

    [Fact]
    public void Every_accident_has_a_distinct_localized_display_card_outside_card_piles()
    {
        var cards = Enum.GetValues<HardToRestrainPower.Effect>()
            .Select(effect => HardToRestrainOutcome.Create(Combat, Player, effect)).ToArray();
        Assert.Equal(5, cards.Select(card => card.GetType()).Distinct().Count());
        foreach (var card in cards)
        {
            Assert.Null(card.Pile);
            Assert.Contains("意外", card.Title);
            Assert.DoesNotContain("SUNQIAN_UNIVERSE", card.GetDescriptionForPile(PileType.Play));
        }
        var conversion = cards.OfType<HardToRestrainConvertBlock>().Single();
        var description = conversion.GetDescriptionForPile(PileType.Play);
        Assert.Contains("你失去所有", description);
        Assert.Contains("并获得等量", description);
        Assert.Contains("\n", description);
        Assert.Contains("三弟", description);
    }

    private async Task<HardToRestrainPower> SummonSkill()
    {
        await Play(await AddToHand<UnstableFactor>());
        var result = await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, 4, null);
        return result.Creature!.GetPower<HardToRestrainPower>()!;
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 5)]
    public async Task Power_loses_hp_and_summons_at_turn_start_with_pending_skill(bool upgraded, int summon)
    {
        var card = await AddToHand<UnstableFactor>();
        if (upgraded) Upgrade(card);
        await Play(card);
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId));
        var power = Player.Creature.GetPower<UnstableFactorPower>()!;
        Assert.Contains("不稳定因素", power.Title.GetFormattedText());
        Assert.Contains("生命", power.Description.GetFormattedText());
        int hp = Player.Creature.CurrentHp;
        await power.AfterSideTurnStart(CombatSide.Enemy, [EnemyAt(0)], Combat);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        await power.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(hp - 2, Player.Creature.CurrentHp);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.Equal(summon, pet.MaxHp);
        Assert.Equal(2, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        Assert.False(pet.HasPower<SharedLifePower>());
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player, CompanionSummons.SecondBrotherId));
        Assert.True(pet.HasPower<HardToRestrainPower>());
        Assert.False(Player.Creature.HasPower<HardToRestrainPower>());
    }

    [Fact]
    public async Task Ineligible_effects_are_excluded_and_immunity_persists_after_replaying_power()
    {
        var skill = await SummonSkill();
        Assert.DoesNotContain(HardToRestrainPower.Effect.Burn, skill.GetEligibleEffects());
        Assert.DoesNotContain(HardToRestrainPower.Effect.ConvertBlock, skill.GetEligibleEffects());
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.StopLifeLoss);
        Assert.DoesNotContain(HardToRestrainPower.Effect.StopLifeLoss, skill.GetEligibleEffects());
        Assert.True(UnstableFactorPower.IsLifeLossDisabled(Player));
        await PowerCmd.Remove<UnstableFactorPower>(Player.Creature);
        await Play(await AddToHand<UnstableFactor>());
        int hp = Player.Creature.CurrentHp;
        await Player.Creature.GetPower<UnstableFactorPower>()!.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(8, skill.Owner.MaxHp);
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
    }

    [Fact]
    public async Task Tinder_is_consumed_and_exactly_doubled_as_burning()
    {
        var skill = await SummonSkill();
        await PowerCmd.Apply<TinderPower>(Choice(), Player.Creature, 7, Player.Creature, null);
        Assert.Contains(HardToRestrainPower.Effect.Burn, skill.GetEligibleEffects());
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.Burn);
        Assert.False(Player.Creature.HasPower<TinderPower>());
        Assert.Equal(14, EnemyAt(0).GetPower<BurningPower>()!.Amount);
        Assert.DoesNotContain(HardToRestrainPower.Effect.Burn, skill.GetEligibleEffects());
    }

    [Fact]
    public async Task Block_is_converted_to_third_brother_max_hp_and_player_temporary_health()
    {
        var skill = await SummonSkill();
        await CreatureCmd.GainBlock(Player.Creature, 9, ValueProp.Unpowered, null);
        int hp = Player.Creature.CurrentHp;
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.ConvertBlock);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Equal(13, skill.Owner.MaxHp);
        Assert.Equal(9, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(hp + 9, Player.Creature.CurrentHp);
        Assert.False(skill.Owner.HasPower<TemporaryHealthPower>());
        Assert.DoesNotContain(HardToRestrainPower.Effect.ConvertBlock, skill.GetEligibleEffects());
    }

    [Fact]
    public async Task Wine_is_on_top_with_both_keywords_and_is_not_upgraded()
    {
        var skill = await SummonSkill();
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.Wine);
        var wine = Assert.IsType<Wine>(Player.PlayerCombatState!.DrawPile.Cards.First());
        Assert.Contains(SquKeywords.Enthralled, wine.Keywords);
        Assert.Contains(CardKeyword.Exhaust, wine.Keywords);
        Assert.False(wine.IsUpgraded);
    }

    [Fact]
    public async Task Attack_is_autoplayed_for_free_but_unplayable_attacks_are_excluded()
    {
        var skill = await SummonSkill();
        foreach (var existing in Player.PlayerCombatState!.Hand.Cards.ToArray())
            await CardPileCmd.Add(existing, PileType.Discard);
        var attack = await AddToHand<StrikeIronclad>();
        await PlayerCmd.SetEnergy(0, Player);
        Assert.Contains(HardToRestrainPower.Effect.Attack, skill.GetEligibleEffects());
        int hp = EnemyAt(0).CurrentHp;
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.Attack);
        Assert.Equal(hp - 6, EnemyAt(0).CurrentHp);
        Assert.NotEqual(PileType.Hand, attack.Pile?.Type);
        var unplayable = await AddToHand<StrikeIronclad>();
        CardCmd.ApplyKeyword(unplayable, CardKeyword.Unplayable);
        Assert.DoesNotContain(HardToRestrainPower.Effect.Attack, skill.GetEligibleEffects());
    }

    [Fact]
    public async Task Turn_end_triggers_only_on_owner_side_and_dead_pet_does_not_trigger()
    {
        var skill = await SummonSkill();
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.StopLifeLoss);
        // No attacks, Tinder, or Block: Wine is the only eligible effect.
        int count = Player.PlayerCombatState!.DrawPile.Cards.Count;
        await skill.BeforeSideTurnEndVeryEarly(Choice(), CombatSide.Enemy, [EnemyAt(0)]);
        Assert.Equal(count, Player.PlayerCombatState.DrawPile.Cards.Count);
        await skill.BeforeSideTurnEndVeryEarly(Choice(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(count + 1, Player.PlayerCombatState.DrawPile.Cards.Count);
        await CreatureCmd.Kill(skill.Owner, force: true);
        Assert.Empty(skill.GetEligibleEffects());
    }

    [Fact]
    public async Task Enthralled_wine_does_not_block_automatic_attack()
    {
        var skill = await SummonSkill();
        foreach (var card in Player.PlayerCombatState!.Hand.Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard);
        var wine = await AddToHand<Wine>();
        CardCmd.ApplyKeyword(wine, SquKeywords.Enthralled);
        await AddToHand<StrikeIronclad>();
        Assert.Contains(HardToRestrainPower.Effect.Attack, skill.GetEligibleEffects());
        int hp = EnemyAt(0).CurrentHp;
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.Attack);
        Assert.Equal(hp - 6, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Real_turn_end_places_generated_wine_in_next_hand()
    {
        var skill = await SummonSkill();
        await skill.ExecuteEffect(Choice(), HardToRestrainPower.Effect.StopLifeLoss);
        await EndTurn();
        Assert.Contains(Player.PlayerCombatState!.Hand.Cards, card => card is Wine
            && card.Keywords.Contains(SquKeywords.Enthralled) && card.Keywords.Contains(CardKeyword.Exhaust));
    }

    [Fact]
    public async Task Mixed_power_plays_stack_hp_loss_and_summon_without_duplicate_skill()
    {
        await Play(await AddToHand<UnstableFactor>());
        await Play(Upgrade(await AddToHand<UnstableFactor>()));
        int hp = Player.Creature.CurrentHp;
        await Player.Creature.GetPower<UnstableFactorPower>()!.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(hp - 4, Player.Creature.CurrentHp);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.Equal(9, pet.MaxHp);
        Assert.Equal(4, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        Assert.Single(pet.Powers.OfType<HardToRestrainPower>());
    }

    [Fact]
    public async Task Prevented_unstable_factor_hp_loss_does_not_raise_contract_cap()
    {
        await Play(await AddToHand<UnstableFactor>());
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.BufferPower>(Choice(), Player.Creature, 1, Player.Creature, null);
        int hp = Player.Creature.CurrentHp;
        await Player.Creature.GetPower<UnstableFactorPower>()!.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.Equal(4, pet.MaxHp);
        Assert.False(pet.HasPower<SharedLifePower>());
    }
}
