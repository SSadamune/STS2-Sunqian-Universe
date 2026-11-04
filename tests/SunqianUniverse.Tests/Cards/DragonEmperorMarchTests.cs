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

public sealed class DragonEmperorMarchTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().WithSeed("dragon-emperor-march");
    private static BlockingPlayerChoiceContext Choice() => new();
    private async Task Second() => await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 20, null);
    private EmperorsMarch[] Tokens => Player.PlayerCombatState!.Hand.Cards.OfType<EmperorsMarch>().ToArray();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Power_triggers_itself_after_resolution_with_the_matching_token_upgrade(bool upgraded)
    {
        await Second();
        var card = await AddToHand<DragonEmperorMarch>();
        if (upgraded) Upgrade(card);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Equal(CardType.Power, card.Type);
        Assert.Equal(CardRarity.Rare, card.Rarity);
        await Play(card);
        Assert.Equal(upgraded, Assert.Single(Tokens).IsUpgraded);
        var power = Player.Creature.GetPower<DragonEmperorMarchPower>()!;
        Assert.Equal(1, power.Amount);
        Assert.Equal(upgraded ? 1 : 0, power.UpgradedCount);
        Assert.Equal(upgraded ? 0 : 1, power.NormalCount);
        Assert.Contains(upgraded ? "帝王之征+" : "帝王之征", power.Description.GetFormattedText());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mixed_sources_stack_counts_without_upgrading_the_other_sources_rewards(bool upgradedFirst)
    {
        await Second();
        foreach (bool upgraded in new[] { upgradedFirst, !upgradedFirst })
        {
            var card = await AddToHand<DragonEmperorMarch>();
            if (upgraded) Upgrade(card);
            await Play(card);
        }
        Assert.Equal(3, Tokens.Length);
        var power = Player.Creature.GetPower<DragonEmperorMarchPower>()!;
        Assert.Equal(2, power.Amount);
        Assert.Equal(1, power.NormalCount);
        Assert.Equal(1, power.UpgradedCount);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Equal(upgradedFirst ? 3 : 2, Tokens.Count(card => card.IsUpgraded));
        Assert.Equal(upgradedFirst ? 2 : 3, Tokens.Count(card => !card.IsUpgraded));
        Assert.Contains("1张[gold]帝王之征[/gold]和1张[gold]帝王之征+[/gold]", power.Description.GetFormattedText());
    }

    [Fact]
    public void Dragon_membership_is_tag_based_and_includes_the_homophone_but_not_the_token()
    {
        Assert.Contains(SquCardTags.Dragon, ModelDb.Card<DragonEmperorMarch>().Tags);
        Assert.Contains(SquCardTags.Dragon, ModelDb.Card<GreenDragonCrescentBlade>().Tags);
        Assert.Contains(SquCardTags.Dragon, ModelDb.Card<DeafenMyEars>().Tags);
        Assert.DoesNotContain("龙", ModelDb.Card<DeafenMyEars>().Title);
        Assert.DoesNotContain(SquCardTags.Dragon, ModelDb.Card<EmperorsMarch>().Tags);
        Assert.DoesNotContain(SquCardTags.Dragon, ModelDb.Card<SerpentSpear>().Tags);
    }

    [Fact]
    public async Task Only_a_living_second_brother_enables_generation_and_untagged_cards_do_not_trigger()
    {
        await PlayerCmd.SetEnergy(4, Player);
        await Play(await AddToHand<DragonEmperorMarch>());
        Assert.Empty(Tokens);
        await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.ThirdBrotherId, 20, null);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Empty(Tokens);
        await Second();
        await Play(await AddToHand<DefendLiubei>());
        Assert.Empty(Tokens);
        await Play(await AddToHand<GreenDragonCrescentBlade>());
        Assert.Single(Tokens);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.Kill(pet, force: true);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Single(Tokens);
    }

    [Fact]
    public async Task Deafen_my_ears_generates_the_token_after_its_hand_modifications()
    {
        await Second();
        await Play(await AddToHand<DragonEmperorMarch>());
        var earlier = Assert.Single(Tokens);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.True(SelfHarmSystem.HasSelfHarm(earlier));
        var later = Assert.Single(Tokens.Where(card => card != earlier));
        Assert.False(SelfHarmSystem.HasSelfHarm(later));
        await Play(later, EnemyAt(0));
        Assert.Single(Tokens); // The untagged token does not generate a replacement.
        AssertInPile(later, PileType.Exhaust);
    }

    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 9)]
    public async Task Token_uses_second_brother_strength_and_applies_doom_from_damage_including_block(bool upgraded, int damage)
    {
        var card = await AddToHand<EmperorsMarch>();
        if (upgraded) Upgrade(card);
        Assert.False(card.CanPlay(out _, out _));
        await Second();
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await PowerCmd.Apply<StrengthPower>(Choice(), pet, 2, Player.Creature, null);
        await PowerCmd.Apply<StrengthPower>(Choice(), Player.Creature, 40, Player.Creature, null);
        await CreatureCmd.GainBlock(EnemyAt(0), 3, ValueProp.Unpowered, null);
        int hp = EnemyAt(0).CurrentHp;
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Equal(CardRarity.Token, card.Rarity);
        Assert.Equal(CardType.Attack, card.Type);
        await Play(card, EnemyAt(0));
        Assert.Equal(hp - damage + 3, EnemyAt(0).CurrentHp);
        Assert.Equal(damage, EnemyAt(0).GetPower<DoomPower>()!.Amount);
        AssertInPile(card, PileType.Exhaust);
        Assert.False(Player.Creature.HasPower<DoomPower>());
    }

    [Fact]
    public async Task Token_doom_reflects_spare_the_young_and_old_damage_multiplier()
    {
        await Second();
        await CompanionSkills.UnlockForCompanion<SpareTheYoungAndOldPower>(Choice(), Player, CompanionSummons.SecondBrotherId);
        await CreatureCmd.SetMaxAndCurrentHp(EnemyAt(0), 100);
        await CreatureCmd.SetCurrentHp(EnemyAt(0), 50);
        await Play(await AddToHand<EmperorsMarch>(), EnemyAt(0));
        Assert.Equal(40, EnemyAt(0).CurrentHp);
        Assert.Equal(10, EnemyAt(0).GetPower<DoomPower>()!.Amount);
    }
}
