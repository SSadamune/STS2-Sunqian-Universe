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
            .AddRemotePlayer<Ironclad>()
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
