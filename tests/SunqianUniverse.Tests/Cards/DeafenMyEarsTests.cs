using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class DeafenMyEarsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("deafen-my-ears");

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    public async Task Grants_cost_scaled_self_harm_to_ordinary_cards_and_exhausts(bool upgraded, int amount)
    {
        var target = await AddToHand<DefendLiubei>();
        var card = await AddToHand<DeafenMyEars>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardRarity.Rare, card.Rarity);
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        await Play(card);
        AssertInPile(card, PileType.Exhaust);
        Assert.True(SelfHarmSystem.HasSelfHarm(target));
        Assert.Equal(amount, SelfHarmSystem.GetAmount(target));
        Assert.Contains("自残", target.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(target.HoverTips.OfType<HoverTip>(), tip => tip.Title?.Contains("自残") == true);
        await PlayerCmd.SetEnergy(0, Player);
        int hp = Player.Creature.CurrentHp;
        await Play(target);
        Assert.Equal(hp - amount, Player.Creature.CurrentHp);
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.True(Player.Creature.Block > 0);
        Assert.False(SelfHarmSystem.HasSelfHarm(target));
        Assert.DoesNotContain("自残", target.GetDescriptionForPile(PileType.Discard));
        Assert.Equal(1, target.EnergyCost.GetResolved());
    }

    [Fact]
    public async Task Uses_modified_cost_and_does_not_overwrite_existing_self_harm()
    {
        var target = await AddToHand<DefendLiubei>();
        target.EnergyCost.SetUntilPlayed(2);
        var native = await AddToHand<AwakenSecondBrother>();
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Equal(8m, SelfHarmSystem.GetAmount(target));
        Assert.Equal(8m, SelfHarmSystem.GetAmount(native));
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Equal(8m, SelfHarmSystem.GetAmount(target));
    }

    [Fact]
    public async Task Spent_native_self_harm_can_be_granted_again_with_a_new_value_and_contract_credit()
    {
        var target = await AddToHand<AwakenSecondBrother>();
        await Play(target, EnemyAt(0));
        await CardPileCmd.Add(target, PileType.Hand);
        Assert.False(SelfHarmSystem.HasSelfHarm(target));
        var card = await AddToHand<DeafenMyEars>();
        Upgrade(card);
        await Play(card);
        Assert.True(SelfHarmSystem.HasSelfHarm(target));
        Assert.Equal(6m, SelfHarmSystem.GetAmount(target));
        Assert.Contains(target.HoverTips.OfType<HoverTip>(), tip => tip.Title?.EndsWith("6") == true);
        await PlayerCmd.SetEnergy(0, Player);
        int hp = Player.Creature.CurrentHp;
        await Play(target, EnemyAt(0));
        Assert.Equal(hp - 6, Player.Creature.CurrentHp);
        Assert.Equal(14, LifeContractSystem.GetTotal(Player));
        SelfHarmSystem.ClearCombat(Combat);
        Assert.Equal(8m, SelfHarmSystem.GetAmount(target));
    }

    [Fact]
    public async Task Spent_granted_self_harm_can_be_granted_again_and_does_not_persist_next_combat()
    {
        var target = await AddToHand<DefendLiubei>();
        await Play(await AddToHand<DeafenMyEars>());
        await Play(target);
        await CardPileCmd.Add(target, PileType.Hand);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.True(SelfHarmSystem.HasSelfHarm(target));
        Assert.Equal(4m, SelfHarmSystem.GetAmount(target));
        SelfHarmSystem.ClearCombat(Combat);
        Assert.False(SelfHarmSystem.HasSelfHarm(target));
        Assert.DoesNotContain("自残", target.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Zero_cost_cards_receive_self_harm_zero()
    {
        var target = await AddToHand<DefendLiubei>();
        target.EnergyCost.SetUntilPlayed(0);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.True(SelfHarmSystem.HasSelfHarm(target));
        Assert.Equal(0m, SelfHarmSystem.GetAmount(target));
        int hp = Player.Creature.CurrentHp;
        await Play(target);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.False(SelfHarmSystem.HasSelfHarm(target));
    }

    [Fact]
    public async Task X_cost_card_uses_available_energy_for_grant_and_pays_no_energy_when_self_harming()
    {
        var target = await AddToHand<MegaCrit.Sts2.Core.Models.Cards.Whirlwind>();
        await PlayerCmd.SetEnergy(3, Player);
        await Play(await AddToHand<DeafenMyEars>());
        Assert.Equal(12m, SelfHarmSystem.GetAmount(target));
        Assert.Equal(0, target.EnergyCost.GetAmountToSpend());
        int hp = Player.Creature.CurrentHp;
        await Play(target);
        Assert.Equal(hp - 12, Player.Creature.CurrentHp);
        Assert.Equal(3, Player.PlayerCombatState!.Energy);
    }
}
