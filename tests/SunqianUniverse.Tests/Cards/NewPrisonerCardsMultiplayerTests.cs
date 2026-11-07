using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NewPrisonerCardsMultiplayerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>()
            .AddRemotePlayer<Ironclad>(2)
            .AddRemotePlayer<Ironclad>(3)
            .AddEnemy<BigDummy>()
            .WithSeed("new-prisoner-cards-multiplayer");

    [Fact]
    public async Task Deflect_strike_blocks_an_ally_with_the_allys_own_block_modifiers()
    {
        Player ally = Players[1];
        await ApplyPower<DexterityPower>(ally.Creature, 2);
        var card = await AddToHand<DeflectStrike>();
        card.DynamicVars["TargetKind"].UpdateCardPreview(card, CardPreviewMode.Normal, ally.Creature, true);
        card.DynamicVars.Block.UpdateCardPreview(card, CardPreviewMode.Normal, ally.Creature, true);
        Assert.Contains("7点[gold]格挡", card.GetDescriptionForPile(PileType.Hand));

        await Play(card, ally.Creature);
        Assert.Equal(7, ally.Creature.Block);
    }

    [Fact]
    public async Task Float_power_does_not_touch_another_players_brothers()
    {
        await PlayerCmd.GainEnergy(10, Player);
        Player other = Players[1];
        Creature localPet = (await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        Creature otherPet = (await CompanionSummons.SummonSpecific(
            Choice(), other, CompanionSummons.SecondBrotherId, 20, null)).Creature!;

        await Play(await AddToHand<WhenFloatOneCup>());
        await Play(await AddToHand<Wine>());

        Assert.Equal(3, localPet.GetPowerAmount<VigorPower>());
        Assert.Equal(0, otherPet.GetPowerAmount<VigorPower>());
        Assert.Equal(3, other.Creature.GetPowerAmount<VigorPower>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Float_shares_the_wines_current_vigor_with_both_brothers_and_every_other_player(bool upgraded)
    {
        var second = (await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.SecondBrotherId, 20, null)).Creature!;
        var third = (await CompanionSummons.SummonSpecific(
            Choice(), Player, CompanionSummons.ThirdBrotherId, 20, null)).Creature!;
        var card = await AddToHand<WhenFloatOneCup>();
        if (upgraded) Upgrade(card);
        Assert.EndsWith("你打出的[gold]酒[/gold]会为二弟、三弟和所有盟友提供等量活力。",
            card.GetDescriptionForPile(PileType.Hand));
        await Play(card);
        var power = Player.Creature.GetPower<WhenFloatOneCupPower>()!;
        Assert.Equal(1, power.Amount);
        Assert.Contains("二弟、三弟和所有盟友", power.Description.GetFormattedText());
        var wine = await AddToHand<Wine>();
        wine.DynamicVars[nameof(VigorPower)].BaseValue = 7;
        await Play(wine);
        Assert.Equal(7, second.GetPowerAmount<VigorPower>());
        Assert.Equal(7, third.GetPowerAmount<VigorPower>());
        Assert.All(Players, player => Assert.Equal(7, player.Creature.GetPowerAmount<VigorPower>()));
    }

    [Fact]
    public async Task Float_only_shares_the_holders_wine_and_auto_play_shares_without_recursion()
    {
        await Play(await AddToHand<WhenFloatOneCup>());
        Player other = Players[1];
        await CardCmd.AutoPlay(Choice(), await AddToHand<Wine>(other), null);
        Assert.Equal(0, Player.Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(3, other.Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(0, Players[2].Creature.GetPowerAmount<VigorPower>());
        await PowerCmd.Apply<WhenFloatOneCupPower>(Choice(), other.Creature, 1, other.Creature, null);
        await CardCmd.AutoPlay(Choice(), await AddToHand<Wine>(), null);
        Assert.Equal(3, Player.Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(6, other.Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(3, Players[2].Creature.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Float_stacks_share_scaled_vigor_and_skip_dead_allies()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<WhenFloatOneCup>());
        await Play(Upgrade(await AddToHand<WhenFloatOneCup>()));
        Players[2].Creature.SetCurrentHpInternal(0);
        Assert.True(Players[2].Creature.IsDead);
        await Play(await AddToHand<Wine>());
        Assert.Equal(3, Player.Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(6, Players[1].Creature.GetPowerAmount<VigorPower>());
        Assert.Equal(0, Players[2].Creature.GetPowerAmount<VigorPower>());
    }

    [Fact]
    public async Task Auto_played_deflect_with_explicit_self_target_blocks_the_owner()
    {
        var card = await AddToHand<DeflectStrike>();
        int hp = EnemyAt(0).CurrentHp;
        await CardCmd.AutoPlay(Choice(), card, Player.Creature);

        Assert.Equal(7, Player.Creature.Block);
        Assert.Equal(hp, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Auto_played_deflect_with_explicit_ally_target_blocks_the_ally()
    {
        Player ally = Players[1];
        var card = await AddToHand<DeflectStrike>();
        int hp = EnemyAt(0).CurrentHp;
        await CardCmd.AutoPlay(Choice(), card, ally.Creature);

        Assert.Equal(7, ally.Creature.Block);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Equal(hp, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Another_players_wine_does_not_consume_the_owners_drunk_trigger()
    {
        await PlayerCmd.GainEnergy(10, Player);
        await Play(await AddToHand<CannotGetDrunk>());
        Player other = Players[1];
        Wine otherWine = await AddToHand<Wine>(other);
        await CardCmd.AutoPlay(Choice(), otherWine, null);

        Assert.Equal(1, Player.Creature.GetPowerAmount<CannotGetDrunkPower>());
        var localWine = await AddToHand<Wine>();
        await CardCmd.AutoPlay(Choice(), localWine, null);
        Assert.Empty(PileType.Draw.GetPile(Player).Cards.OfType<Dazed>());
    }

    private static BlockingPlayerChoiceContext Choice() => new();
}
