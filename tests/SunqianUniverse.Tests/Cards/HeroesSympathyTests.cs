using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HeroesSympathyTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("heroes-sympathy");

    private static BlockingPlayerChoiceContext Choice() => new();

    [Fact]
    public async Task Card_stats_exhaust_and_upgrade_removal_are_correct()
    {
        var card = await AddToHand<HeroesSympathy>();
        Assert.Equal(0, card.EnergyCost.Canonical);
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.Contains(ModelDb.CardPool<LiubeiCardPool>().AllCards, c => c.Id == card.Id);

        Upgrade(card);
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
    }

    [Fact]
    public async Task Play_applies_one_weak_to_self_and_creates_a_stackable_power()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<WeakPower>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<HeroesSympathyPower>());

        await Play(await AddToHand<HeroesSympathy>());
        Assert.Equal(2, Player.Creature.GetPowerAmount<WeakPower>());
        Assert.Equal(2, Player.Creature.GetPowerAmount<HeroesSympathyPower>());
    }

    [Fact]
    public async Task Every_attack_before_its_effect_weakens_every_living_enemy()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());

        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));

        Assert.Equal(1, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(1, EnemyAt(1).GetPowerAmount<WeakPower>());
    }

    [Fact]
    public async Task Skills_do_not_trigger_the_attack_weakness()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        await Play(await AddToHand<DefendLiubei>());

        Assert.Equal(0, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(0, EnemyAt(1).GetPowerAmount<WeakPower>());
    }

    [Fact]
    public async Task Stacked_power_applies_its_stack_count_to_each_enemy()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        await Play(await AddToHand<HeroesSympathy>());

        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));

        Assert.Equal(2, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(2, EnemyAt(1).GetPowerAmount<WeakPower>());
    }

    [Fact]
    public async Task Dead_enemies_are_skipped_and_auto_attacks_trigger()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        Creature dead = EnemyAt(1);
        await CreatureCmd.Kill(dead);
        var attack = await AddToHand<StrikeLiubei>();
        await CardCmd.AutoPlay(Choice(), attack, EnemyAt(0));

        Assert.Equal(1, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(0, dead.GetPowerAmount<WeakPower>());
    }

    [Fact]
    public async Task Righteousness_replays_trigger_the_power_for_each_actual_attack()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await ApplyPower<RighteousnessPower>(Player.Creature, 10);
        await Play(await AddToHand<HeroesSympathy>());
        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));

        Assert.Equal(2, EnemyAt(0).GetPowerAmount<WeakPower>());
        Assert.Equal(2, EnemyAt(1).GetPowerAmount<WeakPower>());
    }

    [Fact]
    public async Task Power_is_removed_at_end_of_the_current_turn()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<HeroesSympathyPower>());

        await EndTurn();

        Assert.Null(Player.Creature.GetPower<HeroesSympathyPower>());
    }

    [Fact]
    public async Task Power_does_not_persist_into_a_real_extra_turn()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<HeroesSympathy>());
        await ApplyPower<AmbergrisPower>(Player.Creature, 1);
        int turn = Player.PlayerCombatState!.TurnNumber;
        PlayerCmd.EndTurn(Player, false, () => Task.CompletedTask);
        await WaitFor(
            () => Combat.CurrentSide == CombatSide.Player
                && Player.PlayerCombatState!.TurnNumber > turn,
            "Extra turn did not start.");
        await WaitForIdle();

        Assert.Null(Player.Creature.GetPower<HeroesSympathyPower>());
    }

    [Fact]
    public void Localization_keeps_the_requested_chinese_newline_and_text()
    {
        using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            "res://sunqian-universe/localization/zhs/cards.json"));
        JsonElement root = document.RootElement;
        Assert.Equal("英雄相惜", root.GetProperty("SUNQIAN_UNIVERSE_CARD_HEROES_SYMPATHY.title").GetString());
        Assert.Equal(
            "获得1层虚弱。\n本回合你每打出一张攻击牌，给予所有敌人一层虚弱。",
            root.GetProperty("SUNQIAN_UNIVERSE_CARD_HEROES_SYMPATHY.description").GetString());

        using JsonDocument english = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            "res://sunqian-universe/localization/eng/cards.json"));
        Assert.Equal(
            "Heroes' Sympathy",
            english.RootElement.GetProperty("SUNQIAN_UNIVERSE_CARD_HEROES_SYMPATHY.title").GetString());
        Assert.Equal(
            "Gain 1 Weak.\nThis turn, whenever you play an Attack, apply 1 Weak to all enemies.",
            english.RootElement.GetProperty("SUNQIAN_UNIVERSE_CARD_HEROES_SYMPATHY.description").GetString());
    }
}
