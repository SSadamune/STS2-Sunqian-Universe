using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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

public sealed class PrisonerThreeCardsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("prisoner-three-cards");
    private BlockingPlayerChoiceContext Choice() => new();

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 6)]
    public async Task Know_warfare_gives_fixed_burning_and_temporary_hp_and_upgraded_block_and_vigor(bool upgraded, int amount)
    {
        var card = await AddToHand<KnowWarfare>();
        if (upgraded) Upgrade(card);
        int hp = Player.Creature.CurrentHp;
        await Play(card);
        Assert.Equal(4, Player.Creature.GetPower<BurningPower>()!.Amount);
        Assert.Equal(4, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(hp + 4, Player.Creature.CurrentHp);
        Assert.Equal(amount, Player.Creature.Block);
        Assert.Equal(amount, Player.Creature.GetPower<VigorPower>()!.Amount);
        AssertInPile(card, PileType.Discard);
    }

    [Theory]
    [InlineData(false, 12)]
    [InlineData(true, 16)]
    public async Task Fair_weather_brothers_attacks_with_pet_and_disables_only_guard_for_two_enemy_turns(bool upgraded, int damage)
    {
        var pet = (await OstyCmd.Summon(Choice(), Player, 10, null)).Creature!;
        await ApplyPower<StrengthPower>(Player.Creature, 10);
        var card = await AddToHand<FairWeatherBrothers>();
        if (upgraded) Upgrade(card);
        int enemyHp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(enemyHp - damage, EnemyAt(0).CurrentHp);
        Assert.True(pet.HasPower<DieForYouPower>());
        Assert.True(pet.HasPower<LifeContractPower>());
        Assert.EndsWith("disabledDescription", pet.GetPower<DieForYouPower>()!.Description.LocEntryKey);
        var disabled = pet.GetPower<BrotherEstrangementPower>()!;
        await disabled.AfterSideTurnEndLate(Choice(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(2, disabled.Amount);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        int hp = Player.Creature.CurrentHp;
        for (int i = 0; i < 2; i++)
        {
            await CreatureCmd.Damage(Choice(), Player.Creature, 2, ValueProp.Move, null, null);
            Assert.Equal(hp - 2 * (i + 1), Player.Creature.CurrentHp);
            Assert.Equal(10, pet.CurrentHp);
            await disabled.AfterSideTurnEndLate(Choice(), CombatSide.Enemy, [EnemyAt(0)]);
        }
        Assert.Null(pet.GetPower<BrotherEstrangementPower>());
        await CreatureCmd.Damage(Choice(), Player.Creature, 2, ValueProp.Move, null, null);
        Assert.Equal(hp - 4, Player.Creature.CurrentHp);
        Assert.Equal(8, pet.CurrentHp);
    }

    [Fact]
    public async Task Fair_weather_brothers_does_not_summon_a_missing_pet()
    {
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<FairWeatherBrothers>(), EnemyAt(0));
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(hp, EnemyAt(0).CurrentHp);
    }

    [Theory]
    [InlineData(false, 0, 9)]
    [InlineData(false, 1, 18)]
    [InlineData(false, 2, 18)]
    [InlineData(true, 0, 9)]
    [InlineData(true, 1, 18)]
    [InlineData(true, 2, 27)]
    public async Task Flying_sword_strike_counts_debuff_kinds_not_stacks_and_draws(bool upgraded, int kinds, int expected)
    {
        if (kinds >= 1) await ApplyPower<BurningPower>(Player.Creature, 8);
        if (kinds >= 2) await ApplyPower<VulnerablePower>(Player.Creature, 4);
        var draw = await AddToHand<DefendLiubei>();
        await CardPileCmd.Add(draw, PileType.Draw, CardPilePosition.Top);
        var card = await AddToHand<FlyingSwordStrike>();
        if (upgraded) Upgrade(card);
        Assert.Contains(CardTag.Strike, card.Tags);
        int hp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - expected, EnemyAt(0).CurrentHp);
        AssertInPile(draw, PileType.Hand);
    }

    [Fact]
    public void All_three_cards_are_registered_in_the_prisoner_pool()
    {
        var pool = ModelDb.CardPool<LiubeiCardPool>().GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint);
        Assert.Single(pool.OfType<KnowWarfare>());
        Assert.Single(pool.OfType<FairWeatherBrothers>());
        Assert.Single(pool.OfType<FlyingSwordStrike>());
    }
}
