using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NewPrisonerCardsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("new-prisoner-cards");

    private static BlockingPlayerChoiceContext Choice() => new();

    [Fact]
    public void Cards_have_the_requested_stats_and_pool()
    {
        foreach ((Type type, int cost, CardType cardType, CardRarity rarity) in new[]
        {
            (typeof(WhenFloatOneCup), 1, CardType.Power, CardRarity.Uncommon),
            (typeof(CannotGetDrunk), 1, CardType.Power, CardRarity.Uncommon),
            (typeof(ParryStrike), 1, CardType.Attack, CardRarity.Common),
        })
        {
            CardModel card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            Assert.Equal(cost, card.EnergyCost.Canonical);
            Assert.Equal(cardType, card.Type);
            Assert.Equal(rarity, card.Rarity);
            Assert.Contains(ModelDb.CardPool<LiubeiCardPool>().AllCards, c => c.Id == card.Id);
            Assert.DoesNotContain(ModelDb.CardPool<SunqianCardPool>().AllCards, c => c.Id == card.Id);
        }

        Assert.Contains(CardTag.Strike, ModelDb.Card<ParryStrike>().Tags);
    }

    [Fact]
    public async Task Float_gains_wine_and_only_existing_owned_brothers_receive_scaled_vigor()
    {
        await PlayerCmd.GainEnergy(10, Player);
        var power = await AddToHand<WhenFloatOneCup>();
        await Play(power);
        Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<Wine>());

        var second = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
        var third = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.ThirdBrotherId, 20, null);
        Assert.NotNull(second.Creature);
        Assert.NotNull(third.Creature);

        await Play(Upgrade(await AddToHand<WhenFloatOneCup>()));
        Assert.Equal(3, Player.Creature.GetPowerAmount<WhenFloatOneCupPower>());
        Assert.Contains(
            "[blue]3[/blue]",
            Player.Creature.GetPower<WhenFloatOneCupPower>()!.Description.GetFormattedText());

        var wine = await AddToHand<Wine>();
        await Play(wine);
        Assert.Equal(9, second.Creature!.GetPowerAmount<VigorPower>());
        Assert.Equal(9, third.Creature!.GetPowerAmount<VigorPower>());
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(1, 2, 3)]
    [InlineData(2, 2, 4)]
    public async Task Float_stack_multipliers_add_together(int first, int second, int expected)
    {
        await PlayerCmd.GainEnergy(10, Player);
        var pet = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
        await ApplyPower<WhenFloatOneCupPower>(Player.Creature, first);
        await ApplyPower<WhenFloatOneCupPower>(Player.Creature, second);
        await Play(await AddToHand<Wine>());
        Assert.Equal(expected * Wine.BaseVigor, pet.Creature!.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Float_does_not_pre_store_for_absent_brothers()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<WhenFloatOneCup>());
        await Play(await AddToHand<Wine>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<WhenFloatOneCupPower>());

        var pet = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
        Assert.Equal(0, pet.Creature!.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Companion_vigor_boosts_and_is_consumed_by_real_attack()
    {
        await PlayerCmd.GainEnergy(10, Player);
        var pet = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
        await Play(await AddToHand<WhenFloatOneCup>());
        await Play(await AddToHand<Wine>());
        Assert.Equal(3, pet.Creature!.GetPowerAmount<VigorPower>());

        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<NominalBrothers>(), EnemyAt(0));
        Assert.Equal(hp - 16, EnemyAt(0).CurrentHp);
        Assert.Equal(0, pet.Creature.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Drunk_power_first_wine_draws_and_suppresses_dazed_then_second_removes_all()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Wine>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        var wine = await TakeWineFromDraw();
        Assert.False(wine.IsUpgraded);
        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;
        await Play(wine);
        Assert.Equal(drawBefore - 1, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        await Play(await AddToHand<Wine>());
        Assert.Null(Player.Creature.GetPower<CannotGetDrunkPower>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    [Fact]
    public async Task Drunk_card_puts_upgraded_wine_plus_in_draw_and_power_text_is_dynamic()
    {
        await PlayerCmd.GainEnergy(10, Player);
        var card = Upgrade(await AddToHand<CannotGetDrunk>());
        Assert.Contains("酒+", card.GetDescriptionForPile(PileType.Hand));
        await Play(card);

        Wine wine = Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Wine>());
        Assert.True(wine.IsUpgraded);
        string one = Player.Creature.GetPower<CannotGetDrunkPower>()!.Description.GetFormattedText();
        Assert.Contains("[blue]1[/blue]", one);
        Assert.Contains("[red]失去[/red]", one);

        await Play(await AddToHand<CannotGetDrunk>());
        string two = Player.Creature.GetPower<CannotGetDrunkPower>()!.Description.GetFormattedText();
        Assert.Contains("[blue]2[/blue]", two);
        Assert.DoesNotContain("[red]失去[/red]", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Drunk_power_stacks_draw_count_and_all_stacks_are_removed_by_second_wine()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        await Play(await AddToHand<CannotGetDrunk>());
        Assert.Equal(2, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        var wine = await TakeWineFromDraw();
        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;
        await Play(wine);
        Assert.Equal(drawBefore - 2, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
        Assert.Equal(2, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        await Play(await AddToHand<Wine>());
        Assert.Null(Player.Creature.GetPower<CannotGetDrunkPower>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    [Fact]
    public async Task Wine_before_power_still_gets_the_first_protected_trigger()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<Wine>());
        await Play(await AddToHand<CannotGetDrunk>());
        var wine = await TakeWineFromDraw();
        int dazedBefore = PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count();

        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;
        await Play(wine);
        Assert.Equal(drawBefore - 1, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Equal(dazedBefore, PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count());
    }

    [Fact]
    public async Task Drunk_triggered_marker_survives_a_stack_and_still_expires_on_next_wine()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        await Play(await AddToHand<Wine>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        await Play(await AddToHand<CannotGetDrunk>());
        Assert.Equal(2, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
        await Play(await AddToHand<Wine>());

        Assert.Null(Player.Creature.GetPower<CannotGetDrunkPower>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    [Fact]
    public async Task Drunk_power_resets_trigger_on_new_turn_but_keeps_stacks()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        await Play(await AddToHand<Wine>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        await EndTurn();
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
        var wine = await AddToHand<Wine>();
        int dazedBefore = PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count();
        await Play(wine);
        Assert.Equal(dazedBefore, PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count());
    }

    [Fact]
    public async Task Float_and_drunk_power_coexist_on_the_same_wine()
    {
        await PlayerCmd.GainEnergy(10, Player);
        var pet = await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
        await Play(await AddToHand<WhenFloatOneCup>());
        await Play(await AddToHand<CannotGetDrunk>());
        var wine = await TakeWineFromDraw();

        await Play(wine);
        Assert.Equal(3, pet.Creature!.GetPowerAmount<VigorPower>());
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
    }

    [Fact]
    public async Task Drunk_trigger_resets_on_a_real_extra_turn_without_an_enemy_turn()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        await Play(await TakeWineFromDraw());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());

        await ApplyPower<AmbergrisPower>(Player.Creature, 1);
        int turn = Player.PlayerCombatState!.TurnNumber;
        PlayerCmd.EndTurn(Player, false, () => Task.CompletedTask);
        await WaitFor(
            () => Combat.CurrentSide == CombatSide.Player
                && Player.PlayerCombatState!.TurnNumber > turn,
            "Extra turn did not start.");
        await WaitForIdle();

        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
        await Play(await AddToHand<Wine>());
        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    [Fact]
    public async Task Drunk_power_handles_real_auto_play_calls()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        var first = await TakeWineFromDraw();
        await CardCmd.AutoPlay(Choice(), first, null);
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());

        var second = await AddToHand<Wine>();
        await CardCmd.AutoPlay(Choice(), second, null);
        Assert.Null(Player.Creature.GetPower<CannotGetDrunkPower>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    [Fact]
    public async Task Parry_strike_righteousness_replays_each_target_branch()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await ApplyPower<RighteousnessPower>(Player.Creature, 10);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<ParryStrike>(), EnemyAt(0));
        Assert.Equal(hp - 18, EnemyAt(0).CurrentHp);
        Assert.Equal(8, Player.Creature.GetPowerAmount<RighteousnessPower>());

        await ApplyPower<RighteousnessPower>(Player.Creature, 2);
        int block = Player.Creature.Block;
        await Play(await AddToHand<ParryStrike>(), Player.Creature);
        Assert.Equal(block + 14, Player.Creature.Block);
        Assert.Equal(8, Player.Creature.GetPowerAmount<RighteousnessPower>());
    }

    [Fact]
    public async Task Card_texts_match_the_requested_lines_and_upgrade_display()
    {
        Assert.Equal(
            "造成9点伤害。\n或者给予玩家7点[gold]格挡[/gold]。",
            ModelDb.Card<ParryStrike>().GetDescriptionForPile(PileType.Hand));
        var parry = Upgrade(await AddToHand<ParryStrike>());
        Assert.Contains("造成13点伤害", parry.GetDescriptionForPile(PileType.Hand));
        Assert.Contains("给予玩家10点[gold]格挡[/gold]", parry.GetDescriptionForPile(PileType.Hand));

        var drunk = await AddToHand<CannotGetDrunk>();
        Assert.Contains("将一张[gold]酒[/gold]放入抽牌堆。", drunk.GetDescriptionForPile(PileType.Hand));
        Assert.DoesNotContain("[red]失去[/red]", drunk.GetDescriptionForPile(PileType.Hand));
        var drunkUp = Upgrade(await AddToHand<CannotGetDrunk>());
        Assert.Contains("将一张[gold]酒+[/gold]放入抽牌堆。", drunkUp.GetDescriptionForPile(PileType.Hand));
    }

    private async Task<Wine> TakeWineFromDraw()
    {
        Wine wine = PileType.Draw.GetPile(Player).Cards.OfType<Wine>().First();
        await CardPileCmd.Add(wine, PileType.Hand);
        await WaitForIdle();
        return wine;
    }

    [Fact]
    public async Task Parry_strike_hits_enemy_or_blocks_player_and_respects_upgrade()
    {
        await PlayerCmd.GainEnergy(10, Player);
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(await AddToHand<ParryStrike>(), enemy);
        Assert.Equal(hp - 9, enemy.CurrentHp);

        var blockCard = await AddToHand<ParryStrike>();
        await Play(blockCard, Player.Creature);
        Assert.True(Player.Creature.Block >= 7);
    }

    [Fact]
    public async Task Parry_strike_upgrade_and_target_branch_use_dynamic_preview_text()
    {
        var card = await AddToHand<ParryStrike>();
        Assert.Contains("造成9点伤害", card.GetDescriptionForPile(PileType.Hand));
        Assert.Contains("格挡", card.GetDescriptionForPile(PileType.Hand));

        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, EnemyAt(0), true);
        string enemyText = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains("造成9点伤害", enemyText);
        Assert.DoesNotContain("格挡", enemyText);

        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, Player.Creature, true);
        string playerText = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains("格挡", playerText);
        Assert.DoesNotContain("造成", playerText);

        Upgrade(card);
        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, EnemyAt(0), true);
        Assert.Contains("造成13点伤害", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Parry_strike_self_preview_and_actual_block_use_owner_dexterity()
    {
        await ApplyPower<DexterityPower>(Player.Creature, 2);
        var card = await AddToHand<ParryStrike>();
        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, Player.Creature, true);
        card.DynamicVars.Block.UpdateCardPreview(card, CardPreviewMode.Normal, Player.Creature, true);
        Assert.Contains("[green]9[/green]点[gold]格挡", card.GetDescriptionForPile(PileType.Hand));

        await Play(card, Player.Creature);
        Assert.Equal(9, Player.Creature.Block);
    }

    [Fact]
    public async Task Parry_strike_damage_preview_and_actual_include_strength_and_vigor()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await ApplyPower<StrengthPower>(Player.Creature, 2);
        await ApplyPower<VigorPower>(Player.Creature, 3);
        var card = await AddToHand<ParryStrike>();
        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, EnemyAt(0), true);
        card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, EnemyAt(0), true);
        Assert.Contains("14", card.GetDescriptionForPile(PileType.Hand));

        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - 14, EnemyAt(0).CurrentHp);
        Assert.Equal(0, Player.Creature.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Float_does_not_pre_store_for_a_dead_and_resummoned_brother()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<WhenFloatOneCup>());
        var first = (await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        await Play(await AddToHand<Wine>());
        Assert.Equal(3, first.GetPowerAmount<VigorPower>());

        await CreatureCmd.Kill(first);
        var revived = (await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        Assert.Equal(0, revived.GetPowerAmount<VigorPower>());

        await Play(await AddToHand<Wine>());
        Assert.Equal(3, revived.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Uproar_auto_play_of_parry_uses_the_resolved_target_branch()
    {
        await PlayerCmd.GainEnergy(10, Player);
        PileType.Draw.GetPile(Player).Clear();
        await AddToDrawPileTop<ParryStrike>();
        var uproar = await AddToHand<Uproar>();
        int enemyHp = EnemyAt(0).CurrentHp;
        int block = Player.Creature.Block;

        await Play(uproar, EnemyAt(0));

        int enemyLost = enemyHp - EnemyAt(0).CurrentHp;
        int blockGained = Player.Creature.Block - block;
        Assert.True(
            enemyLost == 21 || (enemyLost == 12 && blockGained == 7),
            $"Unexpected Uproar resolution: enemyLost={enemyLost}, blockGained={blockGained}");
    }

    [Fact]
    public async Task Parry_strike_is_an_auto_played_attack_that_picks_an_enemy_when_target_is_null()
    {
        var card = await AddToHand<ParryStrike>();
        int hp = EnemyAt(0).CurrentHp;
        await CardCmd.AutoPlay(Choice(), card, null);
        Assert.Equal(hp - 9, EnemyAt(0).CurrentHp);
    }
}

