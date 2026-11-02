using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SelfHarmAndContractTests : SunqianCombatTestSuite
{
    [Fact]
    public async Task Triggered_self_harm_disappears_only_for_that_copy_and_returns_next_combat()
    {
        CardModel card = await AddToHand<AwakenSecondBrother>();
        Assert.Contains("[gold]自残[/gold]", card.GetDescriptionForPile(PileType.Hand));
        await Play(card, EnemyAt(0));
        Assert.False(SelfHarmSystem.HasSelfHarm(card));
        Assert.DoesNotContain("自残", card.GetDescriptionForPile(PileType.Discard));
        Assert.False(card.GetDescriptionForPile(PileType.Discard).StartsWith("\n"));
        Assert.DoesNotContain(card.HoverTips.OfType<HoverTip>(), tip => tip.Title?.StartsWith("自残") == true);
        CardModel other = await AddToHand<AwakenSecondBrother>();
        Assert.Contains("[gold]自残[/gold]", other.GetDescriptionForPile(PileType.Hand));
        SelfHarmSystem.ClearCombat(Combat);
        Assert.Contains("[gold]自残[/gold]", card.GetDescriptionForPile(PileType.Discard));
    }
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("self-harm-and-life-contract");

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 11)]
    public async Task First_play_is_green_zero_cost_and_pays_hp_then_summons_and_attacks(bool upgraded, int amount)
    {
        var card = await AddToHand<AwakenSecondBrother>();
        if (upgraded) Upgrade(card);
        await PlayerCmd.SetEnergy(0, Player);
        int hp = Player.Creature.CurrentHp;
        int enemyHp = EnemyAt(0).CurrentHp;
        Assert.Equal(2, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Equal(CardCostColor.Decreased, CardCostHelper.GetEnergyCostColor(card, Combat));
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - amount, Player.Creature.CurrentHp);
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        Assert.Equal(amount, pet.CurrentHp);
        Assert.Equal(enemyHp - amount, EnemyAt(0).CurrentHp);
        Assert.Equal(amount, LifeContractSystem.GetTotal(Player));
        Assert.Equal(amount, pet.GetPower<LifeContractPower>()!.DisplayAmount);
        Assert.Equal(2, card.EnergyCost.GetResolved());
    }

    [Theory]
    [InlineData(8)]
    [InlineData(7)]
    public async Task At_or_below_threshold_card_costs_energy_without_self_harm(int hp)
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, hp);
        var card = await AddToHand<AwakenSecondBrother>();
        Assert.Equal(2, card.EnergyCost.GetResolved());
        int energy = Player.PlayerCombatState!.Energy;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(energy - 2, Player.PlayerCombatState.Energy);
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Hp_changes_toggle_free_cost_until_first_actual_play()
    {
        var card = await AddToHand<AwakenSecondBrother>();
        await CreatureCmd.SetCurrentHp(Player.Creature, 9);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        await CreatureCmd.SetCurrentHp(Player.Creature, 8);
        Assert.Equal(2, card.EnergyCost.GetResolved());
        await CreatureCmd.SetCurrentHp(Player.Creature, 20);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        await Play(card, EnemyAt(0));
        await CardPileCmd.Add(card, PileType.Hand);
        Assert.Equal(2, card.EnergyCost.GetResolved());
        await CreatureCmd.Heal(Player.Creature, 30, playAnim: false);
        Assert.Equal(2, card.EnergyCost.GetResolved());
    }

    [Fact]
    public async Task Play_below_threshold_preserves_self_harm_for_a_later_eligible_play()
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, 8);
        var card = await AddToHand<AwakenSecondBrother>();
        await Play(card, EnemyAt(0));
        Assert.True(SelfHarmSystem.HasSelfHarm(card));
        Assert.Contains("自残", card.GetDescriptionForPile(PileType.Discard));
        await CardPileCmd.Add(card, PileType.Hand);
        await CreatureCmd.SetCurrentHp(Player.Creature, 40);
        await PlayerCmd.SetEnergy(3, Player);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        await Play(card, EnemyAt(0));
        Assert.Equal(32, Player.Creature.CurrentHp);
        Assert.Equal(3, Player.PlayerCombatState!.Energy);
        Assert.Equal(8, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Separate_copies_pay_once_each_while_replays_do_not_pay_again()
    {
        int hp = Player.Creature.CurrentHp;
        var first = await AddToHand<AwakenSecondBrother>();
        first.BaseReplayCount = 1;
        await Play(first, EnemyAt(0));
        Assert.Equal(hp - 8, Player.Creature.CurrentHp);
        Assert.Equal(16, CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!.MaxHp);
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        Assert.Equal(hp - 16, Player.Creature.CurrentHp);
        Assert.Equal(16, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task New_combat_restores_first_play_eligibility_for_the_same_card()
    {
        var card = await AddToHand<AwakenSecondBrother>();
        await Play(card, EnemyAt(0));
        Assert.False(SelfHarmSystem.IsActive(card));
        SelfHarmSystem.ClearCombat(Combat);
        Assert.True(SelfHarmSystem.IsActive(card));
    }

    [Fact]
    public async Task Actual_loss_after_tungsten_rod_is_credited_to_contract()
    {
        AddRelic<TungstenRod>();
        int hp = Player.Creature.CurrentHp;
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        Assert.Equal(hp - 7, Player.Creature.CurrentHp);
        Assert.Equal(7, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Buffer_prevents_life_payment_and_contract_credit_but_still_uses_free_play()
    {
        await ApplyPower<BufferPower>(Player.Creature, 1);
        int hp = Player.Creature.CurrentHp;
        var card = await AddToHand<AwakenSecondBrother>();
        await PlayerCmd.SetEnergy(0, Player);
        await Play(card, EnemyAt(0));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
        Assert.Equal(2, card.EnergyCost.GetResolved());
        Assert.DoesNotContain("自残", card.GetDescriptionForPile(PileType.Discard));
    }

    [Fact]
    public async Task Temporary_hp_loss_counts_towards_the_contract()
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, 20);
        await ApplyPower<TemporaryHealthPower>(Player.Creature, 10);
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        Assert.Equal(22, Player.Creature.CurrentHp);
        Assert.Equal(2, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(8, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Pet_is_the_damage_dealer_and_owner_strength_is_not_used()
    {
        var pet = (await CompanionSummons.SummonSpecific(new BlockingPlayerChoiceContext(), Player,
            CompanionSummons.SecondBrotherId, 1, null)).Creature!;
        await ApplyPower<StrengthPower>(pet, 3);
        await ApplyPower<StrengthPower>(Player.Creature, 10);
        int hp = EnemyAt(0).CurrentHp;
        var card = await AddToHand<AwakenSecondBrother>();
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 11, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Surviving_pet_heals_owner_at_native_combat_end_once()
    {
        int hp = Player.Creature.CurrentHp;
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        var power = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!
            .GetPower<LifeContractPower>()!;
        var tip = Assert.Single(power.HoverTips.OfType<HoverTip>().Where(tip => tip.Title == power.Title.GetFormattedText()));
        Assert.Contains("8", tip.Description);
        await Hook.AfterCombatEnd(Player.RunState, Combat, new CombatRoom((CombatState)Combat));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Dead_second_brother_does_not_heal_but_revived_second_brother_can_use_accumulated_contract()
    {
        int hp = Player.Creature.CurrentHp;
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.Kill(pet, force: true);
        var power = pet.GetPower<LifeContractPower>()!;
        await power.AfterCombatEnd(null!);
        Assert.Equal(hp - 8, Player.Creature.CurrentHp);
        await CompanionSummons.SummonSpecific(new BlockingPlayerChoiceContext(), Player,
            CompanionSummons.SecondBrotherId, 3, null);
        await power.AfterCombatEnd(null!);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        await CreatureCmd.SetCurrentHp(Player.Creature, hp - 1);
        await power.AfterCombatEnd(null!);
        Assert.Equal(hp - 1, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Unrelated_hp_loss_is_not_a_contract_contribution()
    {
        await CompanionSummons.SummonSpecific(new BlockingPlayerChoiceContext(), Player,
            CompanionSummons.SecondBrotherId, 8, null);
        await Play(await AddToHand<StandByRighteousness>());
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Contract_contributions_are_player_scoped_and_support_future_sources()
    {
        var other = Players.Single(player => player != Player);
        await Play(await AddToHand<AwakenSecondBrother>(), EnemyAt(0));
        LifeContractSystem.AddContribution(other, 5);
        LifeContractSystem.AddContribution(Player, 3);
        Assert.Equal(11, LifeContractSystem.GetTotal(Player));
        Assert.Equal(5, LifeContractSystem.GetTotal(other));
    }

    [Fact]
    public async Task Dynamic_self_harm_title_updates_with_upgrade_and_starter_deck_contains_card()
    {
        Assert.Single(Player.Deck.Cards.OfType<AwakenSecondBrother>());
        var card = await AddToHand<AwakenSecondBrother>();
        var tip = (HoverTip)SquKeywords.CreateSelfHarmHoverTip(card.DynamicVars["SelfHarm"]);
        Assert.EndsWith("8", tip.Title);
        Upgrade(card);
        var upgraded = (HoverTip)SquKeywords.CreateSelfHarmHoverTip(card.DynamicVars["SelfHarm"]);
        Assert.EndsWith("11", upgraded.Title);
        Assert.Contains("11", upgraded.Description);
        Assert.DoesNotContain("8", upgraded.Description);
    }

    private T AddRelic<T>() where T : RelicModel
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        return relic;
    }
}
