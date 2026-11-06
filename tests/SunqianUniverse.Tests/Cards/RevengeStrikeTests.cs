using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RevengeStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<SingleAttackMoveMonster>()
            .WithSeed("revenge-strike");

    [Theory]
    [InlineData(false, 7, 1)]
    [InlineData(true, 9, 2)]
    public async Task Stats_pool_tag_and_dynamic_values_match_the_spec(bool upgraded, int damage, int turns)
    {
        var card = await AddToHand<RevengeStrike>();
        if (upgraded)
        {
            Upgrade(card);
        }

        Assert.Equal(1, card.EnergyCost.Canonical);
        Assert.Equal(CardType.Attack, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Contains(CardTag.Strike, card.Tags);
        Assert.Equal(damage, card.DynamicVars.Damage.IntValue);
        Assert.Equal(turns, card.DynamicVars[RevengeStrike.TurnsVarName].IntValue);
        Assert.Contains(
            ModelDb.CardPool<LiubeiCardPool>().AllCards,
            poolCard => poolCard.Id == card.Id);
        Assert.DoesNotContain(
            ModelDb.CardPool<SunqianCardPool>().AllCards,
            poolCard => poolCard.Id == card.Id);

        var tracker = Player.Creature.GetPower<RevengeStrikeTrackerPower>();
        Assert.NotNull(tracker);
        Assert.False(tracker!.IsVisible);
    }

    [Theory]
    [InlineData(false, 7, 1)]
    [InlineData(true, 9, 2)]
    public async Task No_losses_deal_only_the_base_damage(bool upgraded, int damage, int turns)
    {
        ResetPlayerHp();
        var card = await AddToHand<RevengeStrike>();
        if (upgraded)
        {
            Upgrade(card);
        }

        Assert.Equal(turns, card.DynamicVars[RevengeStrike.TurnsVarName].IntValue);
        Assert.Contains("增加等量伤害(+0)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - damage, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task First_turn_loss_is_counted_even_without_a_turn_end_boundary()
    {
        ResetPlayerHp();
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        var card = await AddToHand<RevengeStrike>();

        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Contains("增加等量伤害(+5)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - 12, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Normal_enemy_turn_loss_is_counted_in_the_next_default_turn()
    {
        ResetPlayerHp();
        await EndTurn();

        Assert.Equal(1, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var card = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+1)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 8, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Extra_turn_window_excludes_the_previous_turn_without_an_enemy_turn()
    {
        ResetPlayerHp();
        await ApplyPower<AmbergrisPower>(Player.Creature, 1);
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        int hpBeforeEnd = Player.Creature.CurrentHp;
        await EndTurnIncludingExtraTurns();

        Assert.Equal(CombatSide.Player, Combat.CurrentSide);
        Assert.Equal(hpBeforeEnd, Player.Creature.CurrentHp);
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        var baseCard = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+0)。", baseCard.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(baseCard, EnemyAt(0));
        Assert.Equal(hp - 7, EnemyAt(0).CurrentHp);

        await DamagePlayer(3, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        Assert.Equal(3, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var upgraded = Upgrade(await AddToHand<RevengeStrike>());
        Assert.Contains("增加等量伤害(+8)。", upgraded.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(upgraded, EnemyAt(0));
        Assert.Equal(hp - 17, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Consecutive_extra_turns_advance_the_upgrade_window_without_enemy_damage()
    {
        ResetPlayerHp();
        await ApplyPower<AmbergrisPower>(Player.Creature, 2);
        await DamagePlayer(4, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        int hpBeforeFirstEnd = Player.Creature.CurrentHp;
        await EndTurnIncludingExtraTurns();

        Assert.Equal(CombatSide.Player, Combat.CurrentSide);
        Assert.Equal(hpBeforeFirstEnd, Player.Creature.CurrentHp);
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(4, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        await DamagePlayer(3, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        int hpBeforeSecondEnd = Player.Creature.CurrentHp;
        await EndTurnIncludingExtraTurns();

        Assert.Equal(CombatSide.Player, Combat.CurrentSide);
        Assert.Equal(hpBeforeSecondEnd, Player.Creature.CurrentHp);
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(3, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        await DamagePlayer(2, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        Assert.Equal(2, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        var baseCard = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+2)。", baseCard.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(baseCard, EnemyAt(0));
        Assert.Equal(hp - 9, EnemyAt(0).CurrentHp);

        var upgraded = Upgrade(await AddToHand<RevengeStrike>());
        Assert.Contains("增加等量伤害(+5)。", upgraded.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(upgraded, EnemyAt(0));
        Assert.Equal(hp - 14, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Alternating_normal_and_extra_turns_expire_the_window_correctly()
    {
        ResetPlayerHp();
        await DamagePlayer(4, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        await EndTurn();

        Assert.Equal(1, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        await DamagePlayer(3, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        await ApplyPower<AmbergrisPower>(Player.Creature, 1);
        int hpBeforeExtra = Player.Creature.CurrentHp;
        await EndTurnIncludingExtraTurns();

        Assert.Equal(CombatSide.Player, Combat.CurrentSide);
        Assert.Equal(hpBeforeExtra, Player.Creature.CurrentHp);
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(4, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        await DamagePlayer(2, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        Assert.Equal(2, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(6, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        var baseCard = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+2)。", baseCard.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(baseCard, EnemyAt(0));
        Assert.Equal(hp - 9, EnemyAt(0).CurrentHp);

        var upgraded = Upgrade(await AddToHand<RevengeStrike>());
        Assert.Contains("增加等量伤害(+6)。", upgraded.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(upgraded, EnemyAt(0));
        Assert.Equal(hp - 15, EnemyAt(0).CurrentHp);

        int hpBeforeNormal = Player.Creature.CurrentHp;
        await EndTurnIncludingExtraTurns();

        Assert.Equal(hpBeforeNormal - 1, Player.Creature.CurrentHp);
        Assert.Equal(1, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(3, RevengeStrikeTrackerPower.GetLostHp(Player, 2));

        var afterNormal = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+1)。", afterNormal.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(afterNormal, EnemyAt(0));
        Assert.Equal(hp - 8, EnemyAt(0).CurrentHp);

        var afterNormalUpgraded = Upgrade(await AddToHand<RevengeStrike>());
        Assert.Contains("增加等量伤害(+3)。", afterNormalUpgraded.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(afterNormalUpgraded, EnemyAt(0));
        Assert.Equal(hp - 12, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Player_and_enemy_turn_losses_use_the_exact_turn_end_windows()
    {
        ResetPlayerHp();
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        await EndTurn();

        Assert.Equal(1, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(6, RevengeStrikeTrackerPower.GetLostHp(Player, 2));
        var baseCard = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+1)。", baseCard.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(baseCard, EnemyAt(0));
        Assert.Equal(hp - 8, EnemyAt(0).CurrentHp);

        var upgraded = Upgrade(await AddToHand<RevengeStrike>());
        Assert.Contains("增加等量伤害(+6)。", upgraded.GetDescriptionForPile(PileType.Hand));
        hp = EnemyAt(0).CurrentHp;
        await Play(upgraded, EnemyAt(0));
        Assert.Equal(hp - 15, EnemyAt(0).CurrentHp);

        await DamagePlayer(3, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        await EndTurn();

        Assert.Equal(1, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 2));
        var later = await AddToHand<RevengeStrike>();
        hp = EnemyAt(0).CurrentHp;
        await Play(later, EnemyAt(0));
        Assert.Equal(hp - 8, EnemyAt(0).CurrentHp);
    }

    [Theory]
    [InlineData(10, 5, 12, 7)]
    [InlineData(4, 0, 10, 10)]
    [InlineData(0, 10, 6, 0)]
    public async Task Block_and_temporary_health_count_only_actual_hp_lost(
        int temporary, int block, int damage, int expectedLoss)
    {
        ResetPlayerHp();
        var context = new BlockingPlayerChoiceContext();
        if (temporary > 0)
        {
            await PowerCmd.Apply<TemporaryHealthPower>(
                context, Player.Creature, temporary, Player.Creature, null);
        }

        if (block > 0)
        {
            await CreatureCmd.GainBlock(Player.Creature, block, ValueProp.Move, null);
        }

        await DamagePlayer(damage, ValueProp.Move, EnemyAt(0));

        Assert.Equal(expectedLoss, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var card = await AddToHand<RevengeStrike>();
        if (expectedLoss > 0)
        {
            Assert.Contains($"增加等量伤害(+{expectedLoss})。", card.GetDescriptionForPile(PileType.Hand));
        }
        else
        {
            Assert.Contains("增加等量伤害(+0)。", card.GetDescriptionForPile(PileType.Hand));
        }

        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - (7 + expectedLoss), EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task SetCurrentHp_loss_is_counted_as_actual_hp_loss()
    {
        ResetPlayerHp();
        int before = Player.Creature.CurrentHp;
        await CreatureCmd.SetCurrentHp(Player.Creature, before - 6);

        Assert.Equal(6, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var card = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+6)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 13, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Healing_does_not_offset_history()
    {
        ResetPlayerHp();
        await DamagePlayer(10, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        await CreatureCmd.Heal(Player.Creature, 10);

        Assert.Equal(10, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var card = await AddToHand<RevengeStrike>();
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 17, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Self_damage_is_counted()
    {
        ResetPlayerHp();
        await Play(await AddToHand<StandByRighteousness>());

        Assert.Equal(4, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        var card = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+4)。", card.GetDescriptionForPile(PileType.Hand));
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 11, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Overkill_records_only_the_hp_actually_lost()
    {
        Player.Creature.SetCurrentHpInternal(5);
        await DamagePlayer(20, ValueProp.Move, EnemyAt(0));

        Assert.Equal(5, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
    }

    [Fact]
    public async Task Normal_strength_and_vulnerable_modify_the_whole_damage_once()
    {
        ResetPlayerHp();
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        var context = new BlockingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(context, Player.Creature, 2, Player.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(context, EnemyAt(0), 1, Player.Creature, null);
        var card = await AddToHand<RevengeStrike>();
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - 21, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Sharp_enchantment_adds_to_the_base_and_window_damage()
    {
        ResetPlayerHp();
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        var card = await AddToHand<RevengeStrike>();
        CardCmd.Enchant<Sharp>(card, 3);
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - 15, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Righteousness_replays_reevaluate_the_window_between_plays()
    {
        ResetPlayerHp();
        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        var context = new BlockingPlayerChoiceContext();
        await PowerCmd.Apply<RighteousnessPower>(context, Player.Creature, 20, Player.Creature, null);
        await PowerCmd.Apply<ThornsPower>(context, EnemyAt(0), 1, EnemyAt(0), null);
        var card = await AddToHand<RevengeStrike>();
        int hp = EnemyAt(0).CurrentHp;

        await Play(card, EnemyAt(0));

        // 12 + 13 + 14: each replay re-reads the loss added by the previous
        // replay's Thorns, without adding the base damage again.
        Assert.Equal(hp - 39, EnemyAt(0).CurrentHp);
        Assert.Equal(8, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(16, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
    }

    [Fact]
    public async Task Smart_description_updates_in_real_time_inside_combat()
    {
        ResetPlayerHp();
        var card = await AddToHand<RevengeStrike>();
        Assert.Contains("增加等量伤害(+0)。", card.GetDescriptionForPile(PileType.Hand));

        await DamagePlayer(5, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        Assert.Contains("增加等量伤害(+5)。", card.GetDescriptionForPile(PileType.Hand));

        await DamagePlayer(3, ValueProp.Unblockable | ValueProp.Unpowered, Player.Creature);
        Assert.Contains("增加等量伤害(+8)。", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public void Smart_description_is_hidden_on_canonical_cards_outside_combat()
    {
        string outside = ModelDb.Card<RevengeStrike>().GetDescriptionForPile(PileType.Deck);
        Assert.DoesNotContain("(+", outside);
        Assert.Contains("增加等量伤害。", outside);
    }

    [Fact]
    public void Fresh_combat_starts_with_no_recorded_loss()
    {
        Assert.NotNull(Player.Creature.GetPower<RevengeStrikeTrackerPower>());
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 1));
        Assert.Equal(0, RevengeStrikeTrackerPower.GetLostHp(Player, 2));
    }

    [Theory]
    [InlineData("zhs", "造成{Damage:diff()}点伤害。\n根据过去{Turns:diff()}回合内你失去的生命，增加等量伤害{bouns}。")]
    [InlineData("eng", "Deal {Damage:diff()} damage.\nDeal additional damage equal to the HP you lost within the past {Turns:diff()} {Turns:plural:turn|turns} {bouns}.")]
    public void Localization_preserves_the_requested_text_and_newline(string language, string expected)
    {
        using JsonDocument table = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            $"res://sunqian-universe/localization/{language}/cards.json"));
        Assert.Equal(expected, table.RootElement
            .GetProperty("SUNQIAN_UNIVERSE_CARD_REVENGE_STRIKE.description")
            .GetString());
    }

    private async Task EndTurnIncludingExtraTurns()
    {
        if (Combat.CurrentSide != CombatSide.Player)
        {
            throw new InvalidOperationException("The player is not taking a turn.");
        }

        int turnNumber = Player.PlayerCombatState!.TurnNumber;
        PlayerCmd.EndTurn(Player, false, () => Task.CompletedTask);
        await WaitFor(
            () => Combat.CurrentSide == CombatSide.Player
                && Player.PlayerCombatState!.TurnNumber > turnNumber,
            "The next player turn (including an extra turn) did not start in time.");
        await WaitForIdle();
    }

    private void ResetPlayerHp()
    {
        Player.Creature.SetCurrentHpInternal(Player.Creature.MaxHp);
    }

    private Task DamagePlayer(int amount, ValueProp props, Creature? dealer) =>
        CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Player.Creature,
            amount,
            props,
            dealer,
            null,
            null);
}

