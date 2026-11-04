using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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

public sealed class SoulContractTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>().WithSeed("soul-contract");
    private static BlockingPlayerChoiceContext Choice() => new();
    private async Task ChooseBrother(string id, bool upgraded = false)
    {
        var card = await AddToHand<SoulContract>();
        if (upgraded) Upgrade(card);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is CompanionSummonChoice option && option.CompanionId == id);
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(1, selector.SelectionRequestCount);
    }

    [Theory]
    [InlineData(CompanionSummons.SecondBrotherId, false)]
    [InlineData(CompanionSummons.SecondBrotherId, true)]
    [InlineData(CompanionSummons.ThirdBrotherId, false)]
    [InlineData(CompanionSummons.ThirdBrotherId, true)]
    public async Task Choice_summons_six_unlocks_shared_life_and_records_only_the_chosen_brother(string id, bool upgraded)
    {
        int hp = Player.Creature.CurrentHp;
        await ChooseBrother(id, upgraded);
        var pet = CompanionSummons.GetCompanion(Player, id)!;
        Assert.Equal(6, pet.CurrentHp);
        Assert.Equal(6, pet.MaxHp);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.True(pet.GetPower<SharedLifePower>()!.IsActive);
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player, id));
        var record = Assert.Single(CompanionSkillCollection.Pile.GetPile(Player).Cards.OfType<CompanionSkillRecord>());
        Assert.Equal(id, record.CompanionId);
        Assert.Equal("SharedLife", record.Skill);
        Assert.Contains(CompanionSummons.GetName(id).GetFormattedText(), record.Title);
        Assert.Contains("至多恢复0点，此上限为", record.GetDescriptionForPile(CompanionSkillCollection.Pile));
        Assert.DoesNotContain("此召唤物", record.GetDescriptionForPile(CompanionSkillCollection.Pile));
        Assert.DoesNotContain("的它", record.GetDescriptionForPile(CompanionSkillCollection.Pile));
        Assert.False(CompanionSkillCollection.CanActivate(Player, id));
    }

    [Fact]
    public async Task Starter_cost_type_and_upgrade_are_correct_and_preview_is_generic()
    {
        Assert.Single(Player.Deck.Cards.OfType<SoulContract>());
        var card = await AddToHand<SoulContract>();
        Assert.Equal(CardType.Power, card.Type);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Equal(6, card.DynamicVars.Summon.IntValue);
        Assert.DoesNotContain(CardKeyword.Innate, card.Keywords);
        Upgrade(card);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.Equal(6, card.DynamicVars.Summon.IntValue);
        Assert.Contains("此召唤物", ModelDb.Power<SharedLifePower>().Description.GetFormattedText());
        Assert.Contains("至多恢复等量于", ModelDb.Power<SharedLifePower>().Description.GetFormattedText());
    }

    [Theory]
    [InlineData(CompanionSummons.SecondBrotherId)]
    [InlineData(CompanionSummons.ThirdBrotherId)]
    public async Task Dead_companion_can_be_recalled_repeatedly_and_only_actual_player_loss_is_credited(string id)
    {
        await ChooseBrother(id);
        var pet = CompanionSummons.GetCompanion(Player, id)!;
        Assert.False(await CompanionSkillCollection.Activate(Choice(), Player, id));
        int hp = Player.Creature.CurrentHp;
        for (int i = 1; i <= 2; i++)
        {
            await CreatureCmd.Kill(pet, force: true);
            Assert.True(CompanionSkillCollection.CanActivate(Player, id));
            Assert.True(await CompanionSkillCollection.Activate(Choice(), Player, id));
            Assert.Same(pet, CompanionSummons.GetCompanion(Player, id));
            Assert.Equal(8, pet.CurrentHp);
            Assert.Equal(8, pet.MaxHp);
            Assert.Equal(hp - i * 8, Player.Creature.CurrentHp);
            Assert.Equal(i * 8, SharedLifeSystem.GetTotal(Player, id));
        }
        Assert.Single(CompanionSkillCollection.Pile.GetPile(Player).Cards);
        Assert.Contains("至多恢复16点", pet.GetPower<SharedLifePower>()!.Description.GetFormattedText());
        await CreatureCmd.SetCurrentHp(pet, 3);
        int beforeHeal = Player.Creature.CurrentHp;
        await pet.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        Assert.Equal(beforeHeal + 3, Player.Creature.CurrentHp);
        await pet.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        Assert.Equal(beforeHeal + 3, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Ordinary_summons_do_not_grant_shared_life_and_unstable_factor_credit_is_retained_before_unlock()
    {
        var second = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 5, null)).Creature!;
        Assert.False(second.HasPower<SharedLifePower>());
        await Play(await AddToHand<UnstableFactor>());
        await Player.Creature.GetPower<UnstableFactorPower>()!.AfterSideTurnStart(MegaCrit.Sts2.Core.Combat.CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(2, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        var third = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.False(third.HasPower<SharedLifePower>());
        await ChooseBrother(CompanionSummons.ThirdBrotherId);
        Assert.Equal(2, third.GetPower<SharedLifePower>()!.DisplayAmount);
        Assert.True(third.GetPower<SharedLifePower>()!.IsActive);
        Assert.Equal(2, CompanionSkillCollection.Pile.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Block_and_buffer_do_not_inflate_recall_contributions()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.Kill(pet, force: true);
        await CreatureCmd.GainBlock(Player.Creature, 15, ValueProp.Unpowered, null);
        await PowerCmd.Apply<BufferPower>(Choice(), Player.Creature, 1, Player.Creature, null);
        int hp = Player.Creature.CurrentHp;
        Assert.True(await CompanionSkillCollection.Activate(Choice(), Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(15, Player.Creature.Block);
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player));
        Assert.Equal(8, pet.CurrentHp);
    }

    [Fact]
    public async Task Repeated_unlock_is_unique_and_other_skills_also_enter_the_collection()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        Assert.Equal(12, pet.MaxHp);
        Assert.Single(CompanionSkillCollection.Pile.GetPile(Player).Cards);
        await CompanionSkills.UnlockForCompanion<SpareTheYoungAndOldPower>(Choice(), Player, CompanionSummons.SecondBrotherId);
        Assert.Equal(2, CompanionSkillCollection.Pile.GetPile(Player).Cards.Count);
        Assert.Equal(10, pet.GetPower<SpareTheYoungAndOldPower>()!.RequiredMaxHp);
        Assert.True(pet.GetPower<SpareTheYoungAndOldPower>()!.IsActive);
        var other = Players.Single(player => player != Player);
        Assert.Empty(CompanionSkillCollection.Pile.GetPile(other).Cards);
        Assert.False(CompanionSkillCollection.CanActivate(other, CompanionSummons.SecondBrotherId));
    }

    [Fact]
    public async Task Dead_companion_does_not_heal_and_brothers_use_separate_caps()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        await ChooseBrother(CompanionSummons.ThirdBrotherId);
        var second = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        var third = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        SharedLifeSystem.AddContribution(Player, 4, CompanionSummons.SecondBrotherId);
        SharedLifeSystem.AddContribution(Player, 9, CompanionSummons.ThirdBrotherId);
        await CreatureCmd.SetCurrentHp(Player.Creature, 20);
        await CreatureCmd.Kill(second, force: true);
        await second.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        Assert.Equal(20, Player.Creature.CurrentHp);
        await third.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        Assert.Equal(26, Player.Creature.CurrentHp);
        Assert.Equal(4, SharedLifeSystem.GetTotal(Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(2, CompanionSkillCollection.Pile.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Shared_life_in_a_later_slot_obeys_the_max_hp_requirement()
    {
        await Play(await AddToHand<GreenDragonCrescentBlade>());
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        var power = pet.GetPower<SharedLifePower>()!;
        Assert.Equal(10, power.RequiredMaxHp);
        await CreatureCmd.SetMaxHp(pet, 9);
        await CreatureCmd.Kill(pet, force: true);
        Assert.False(CompanionSkillCollection.CanActivate(Player, CompanionSummons.SecondBrotherId));
        Assert.False(await CompanionSkillCollection.Activate(Choice(), Player, CompanionSummons.SecondBrotherId));
    }
}
