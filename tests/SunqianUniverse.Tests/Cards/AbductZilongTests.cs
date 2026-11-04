using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class AbductZilongTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().WithSeed("abduct-zilong");

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    public async Task Discovers_three_other_character_strikes_with_enchantments_and_grants_righteousness(bool upgraded, int righteousness)
    {
        var card = await AddToHand<AbductZilong>();
        if (upgraded) Upgrade(card);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Contains(SquCardTags.Dragon, card.Tags);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Equal(3, options.Count);
            Assert.Equal(3, options.Select(option => option.Id).Distinct().Count());
            foreach (var option in options)
            {
                Assert.Contains(CardTag.Strike, option.Tags);
                Assert.False(CharacterCardPoolMembership.Contains(Player.Character.CardPool, option));
                Assert.NotNull(option.Enchantment);
            }
            selected = options[0];
            return [selected];
        });
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(selected);
        AssertInPile(selected, PileType.Hand);
        Assert.Equal(righteousness, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
        AssertInPile(card, PileType.Exhaust);
        Assert.Contains(upgraded ? "强力附魔" : "随机附魔", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Combat_reward_keeps_the_acquisition_enchantment_after_the_combat_card_changes()
    {
        CardModel? selected = null;
        var selector = new ScriptedCardSelector().ThenSelect(options => { selected = options[0]; return [selected]; });
        using (UseCardSelector(selector)) await Play(await AddToHand<AbductZilong>());
        var snapshot = selected!.ToSerializable();
        CardCmd.ClearEnchantment(selected);
        CardCmd.Enchant<Sharp>(selected, 99);
        var room = new CombatRoom((CombatState)Combat);
        await Hook.AfterCombatEnd(Player.RunState, Combat, room);
        var reward = Assert.IsType<SpecialCardReward>(Assert.Single(room.ExtraRewards[Player]));
        var savedReward = reward.ToSerializable().SpecialCard!;
        Assert.Equal(snapshot.Id, savedReward.Id);
        Assert.Equal(snapshot.Enchantment!.Id, savedReward.Enchantment!.Id);
        Assert.Equal(snapshot.Enchantment.Amount, savedReward.Enchantment.Amount);
        Assert.NotEqual(99, savedReward.Enchantment.Amount);
        EnchantedStrikeRewards.Offer(room);
        Assert.Single(room.ExtraRewards[Player]);
    }

    [Theory]
    [InlineData(typeof(Slither), 0)]
    [InlineData(typeof(Sharp), 8)]
    [InlineData(typeof(Sown), 2)]
    public async Task Rewards_preserve_native_enchantment_types_and_amounts_including_slither(Type type, int amount)
    {
        var card = await AddToHand<Blitzkrieg>();
        AttackEnchantmentPool.ApplySpec(card, new AttackEnchantmentPool.EnchantmentSpec(type, amount));
        EnchantedStrikeRewards.Record(card);
        var original = card.Enchantment!;
        var room = new CombatRoom((CombatState)Combat);
        EnchantedStrikeRewards.Offer(room);
        var reward = Assert.Single(room.ExtraRewards[Player]).ToSerializable().SpecialCard!;
        Assert.Equal(original.Id, reward.Enchantment!.Id);
        Assert.Equal(original.Amount, reward.Enchantment.Amount);
        Assert.Equal(card.CurrentUpgradeLevel, reward.CurrentUpgradeLevel);
    }

    [Fact]
    public async Task Each_play_adds_its_own_reward_and_does_not_add_the_source_card_as_a_reward()
    {
        var selector = new ScriptedCardSelector().ThenSelectFirst().ThenSelectFirst();
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<AbductZilong>());
            await Play(await AddToHand<AbductZilong>());
        }
        AssertAllSelectionsConsumed(selector);
        var room = new CombatRoom((CombatState)Combat);
        EnchantedStrikeRewards.Offer(room);
        Assert.Equal(2, room.ExtraRewards[Player].Count);
        foreach (var reward in room.ExtraRewards[Player])
        {
            var saved = reward.ToSerializable().SpecialCard!;
            Assert.NotEqual(ModelDb.Card<AbductZilong>().Id, saved.Id);
            Assert.NotNull(saved.Enchantment);
        }
    }

    [Fact]
    public async Task Dragon_tag_triggers_dragon_emperor_march_after_discovery()
    {
        await CompanionSummons.SummonSpecific(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            Player, CompanionSummons.SecondBrotherId, 10, null);
        await Play(await AddToHand<DragonEmperorMarch>());
        Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<EmperorsMarch>());
        var selector = new ScriptedCardSelector().ThenSelectFirst();
        using (UseCardSelector(selector)) await Play(await AddToHand<AbductZilong>());
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, Player.PlayerCombatState.Hand.Cards.OfType<EmperorsMarch>().Count());
    }
}
