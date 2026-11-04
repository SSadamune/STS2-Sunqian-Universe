using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
    [InlineData(false, 6)]
    [InlineData(true, 8)]
    public async Task Know_warfare_gives_two_burning_and_upgradable_temporary_hp_and_vigor(bool upgraded, int amount)
    {
        var card = await AddToHand<KnowWarfare>();
        if (upgraded) Upgrade(card);
        int hp = Player.Creature.CurrentHp;
        await Play(card);
        Assert.Equal(2, Player.Creature.GetPower<BurningPower>()!.Amount);
        Assert.Equal(amount, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(hp + amount, Player.Creature.CurrentHp);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Equal(amount, Player.Creature.GetPower<VigorPower>()!.Amount);
        AssertInPile(card, PileType.Discard);
    }

    [Theory]
    [InlineData(false, 13)]
    [InlineData(true, 17)]
    public async Task Nominal_brothers_attacks_with_pet_and_disables_only_guard_for_two_enemy_turns(bool upgraded, int damage)
    {
        var pet = (await OstyCmd.Summon(Choice(), Player, 10, null)).Creature!;
        await ApplyPower<StrengthPower>(Player.Creature, 10);
        var card = await AddToHand<NominalBrothers>();
        if (upgraded) Upgrade(card);
        var tip = Assert.Single(card.HoverTips.OfType<HoverTip>());
        Assert.Equal(ModelDb.Power<DieForYouPower>().Title.GetFormattedText(), tip.Title);
        Assert.NotNull(tip.Icon);
        Assert.Contains("此召唤物", tip.Description);
        Assert.DoesNotContain("奥斯提", tip.Description);
        int enemyHp = EnemyAt(0).CurrentHp;
        await Play(card, EnemyAt(0));
        Assert.Equal(enemyHp - damage, EnemyAt(0).CurrentHp);
        Assert.True(pet.HasPower<DieForYouPower>());
        Assert.False(pet.HasPower<SharedLifePower>());
        Assert.EndsWith("disabledDescription", pet.GetPower<DieForYouPower>()!.Description.LocEntryKey);
        var disabled = pet.GetPower<ActuallyNotClosePower>()!;
        Assert.Equal("其实不熟", disabled.Title.GetFormattedText());
        Assert.NotNull(disabled.Icon);
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
        Assert.Null(pet.GetPower<ActuallyNotClosePower>());
        await CreatureCmd.Damage(Choice(), Player.Creature, 2, ValueProp.Move, null, null);
        Assert.Equal(hp - 4, Player.Creature.CurrentHp);
        Assert.Equal(8, pet.CurrentHp);
    }

    [Fact]
    public async Task Nominal_brothers_cannot_be_played_without_second_brother_and_spends_no_energy()
    {
        int hp = EnemyAt(0).CurrentHp;
        var card = await AddToHand<NominalBrothers>();
        int energy = Player.PlayerCombatState!.Energy;
        Assert.False(card.CanPlay());
        Assert.False(card.TryManualPlay(EnemyAt(0)));
        Assert.Equal(energy, Player.PlayerCombatState.Energy);
        AssertInPile(card, PileType.Hand);
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(hp, EnemyAt(0).CurrentHp);
        await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.OstyId, 10, null);
        Assert.False(card.CanPlay());
    }

    [Fact]
    public async Task Nominal_brothers_requires_living_second_brother_and_becomes_playable_after_reviving()
    {
        var card = await AddToHand<NominalBrothers>();
        var pet = (await OstyCmd.Summon(Choice(), Player, 10, null)).Creature!;
        Assert.True(card.CanPlay());
        await CreatureCmd.Kill(pet);
        Assert.False(card.CanPlay());
        Assert.False(card.TryManualPlay(EnemyAt(0)));
        await OstyCmd.Summon(Choice(), Player, 10, null);
        Assert.True(card.CanPlay());
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Available_brother_fights_and_only_selected_brother_loses_guard(bool both, bool chooseThird, bool upgraded)
    {
        var third = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, 20, null)).Creature!;
        var second = both ? (await OstyCmd.Summon(Choice(), Player, 20, null)).Creature : null;
        var card = await AddToHand<NominalBrothers>();
        Assert.True(card.CanPlay());
        if (upgraded) Upgrade(card);
        var chosen = chooseThird ? third : second!;
        var other = chooseThird ? second : third;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
            [options.OfType<BrotherAttackChoice>().Single(c => c.CompanionId == (chooseThird ? CompanionSummons.ThirdBrotherId : CompanionSummons.SecondBrotherId))]);
        int enemyHp = EnemyAt(0).CurrentHp;
        if (both)
        {
            using (UseCardSelector(selector)) await Play(card, EnemyAt(0));
            AssertAllSelectionsConsumed(selector);
        }
        else await Play(card, EnemyAt(0));
        Assert.Equal(enemyHp - (upgraded ? 17 : 13), EnemyAt(0).CurrentHp);
        Assert.True(chosen.HasPower<ActuallyNotClosePower>());
        Assert.False(other?.HasPower<ActuallyNotClosePower>() ?? false);
        if (other != null) await CreatureCmd.Kill(other);
        int hp = Player.Creature.CurrentHp;
        await CreatureCmd.Damage(Choice(), Player.Creature, 2, ValueProp.Move, null, null);
        Assert.Equal(hp - 2, Player.Creature.CurrentHp);
        Assert.Equal(20, chosen.CurrentHp);
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
        Assert.Single(pool.OfType<NominalBrothers>());
        Assert.Single(pool.OfType<FlyingSwordStrike>());
    }

    [Fact]
    public void Renamed_card_and_power_use_current_ids()
    {
        var card = ModelDb.Card<NominalBrothers>();
        var power = ModelDb.Power<ActuallyNotClosePower>();
        Assert.Equal("SUNQIAN_UNIVERSE_CARD_NOMINAL_BROTHERS", card.Id.Entry);
        Assert.Equal("名义兄弟", card.Title);
        Assert.Equal("SUNQIAN_UNIVERSE_POWER_ACTUALLY_NOT_CLOSE_POWER", power.Id.Entry);
    }
}
