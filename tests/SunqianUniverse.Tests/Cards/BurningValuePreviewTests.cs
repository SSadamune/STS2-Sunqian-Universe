using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BurningValuePreviewTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("burning-value-preview");
    private static void Preview(CardModel card) => card.DynamicVars[nameof(BurningPower)]
        .UpdateCardPreview(card, CardPreviewMode.Normal, null, true);

    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 13)]
    public async Task Null_target_preview_is_green_and_matches_actual_random_enemy_application(int tinder, int expected)
    {
        await ApplyPower<GoodFirePower>(Player.Creature, 160);
        if (tinder > 0) await ApplyPower<TinderPower>(Player.Creature, tinder);
        var card = await AddToHand<WatchFireFromShore>();
        var other = await AddToHand<WatchFireFromShore>();
        Preview(card);
        Preview(other);
        Assert.Equal(2m, card.DynamicVars[nameof(BurningPower)].BaseValue);
        Assert.Contains($"[green]{expected}[/green]", card.GetDescriptionForPile(PileType.Hand));
        Assert.Contains($"[green]{expected}[/green]", other.GetDescriptionForPile(PileType.Hand));
        await Play(card);
        Assert.Equal(expected, EnemyAt(0).GetPower<BurningPower>()!.Amount);
        Assert.Equal(expected, EnemyAt(1).GetPower<BurningPower>()!.Amount);
        Assert.False(Player.Creature.HasPower<TinderPower>());
    }

    [Theory]
    [InlineData(PileType.Hand, 13)]
    [InlineData(PileType.Play, 13)]
    [InlineData(PileType.Draw, 2)]
    [InlineData(PileType.Discard, 2)]
    public async Task Only_hand_and_play_infer_enemy_target_without_hover(PileType pile, int expected)
    {
        var card = await AddToHand<WatchFireFromShore>();
        await ApplyPower<GoodFirePower>(Player.Creature, 160);
        await ApplyPower<TinderPower>(Player.Creature, 3);
        await CardPileCmd.Add(card, pile);
        Preview(card);
        Assert.Equal(expected, (int)card.DynamicVars[nameof(BurningPower)].PreviewValue);
        Assert.Equal(3, Player.Creature.GetPower<TinderPower>()!.Amount);
        card.DynamicVars[nameof(BurningPower)].UpdateCardPreview(card, CardPreviewMode.Normal, Player.Creature, true);
        Assert.Equal(2m, card.DynamicVars[nameof(BurningPower)].PreviewValue);
    }

    [Fact]
    public async Task Self_and_friendly_applications_neither_gain_tinder_nor_bind_it_to_the_source()
    {
        var context = new BlockingPlayerChoiceContext();
        var selfSource = await AddToHand<WatchFireFromShore>();
        var enemySource = await AddToHand<WatchFireFromShore>();
        var pet = (await OstyCmd.Summon(context, Player, 8, null)).Creature!;
        await ApplyPower<TinderPower>(Player.Creature, 3);
        await PowerCmd.Apply<BurningPower>(context, Player.Creature, 2, Player.Creature, selfSource);
        await PowerCmd.Apply<BurningPower>(context, pet, 2, Player.Creature, selfSource);
        Assert.Equal(2, Player.Creature.GetPower<BurningPower>()!.Amount);
        Assert.Equal(2, pet.GetPower<BurningPower>()!.Amount);
        await Play(enemySource);
        Assert.Equal(5, EnemyAt(0).GetPower<BurningPower>()!.Amount);
        Assert.False(Player.Creature.HasPower<TinderPower>());
    }

    [Fact]
    public async Task Custom_burning_variable_keeps_vigor_then_tinder_then_multiplier_order()
    {
        var card = await AddToHand<DigRaid>();
        await ApplyPower<VigorPower>(Player.Creature, 3);
        await ApplyPower<TinderPower>(Player.Creature, 3);
        await ApplyPower<GoodFirePower>(Player.Creature, 160);
        Preview(card);
        Assert.Contains("[green]33[/green]", card.GetDescriptionForPile(PileType.Hand));
        await Play(card, EnemyAt(0));
        Assert.Equal(33, EnemyAt(0).GetPower<BurningPower>()!.Amount);
    }
}
