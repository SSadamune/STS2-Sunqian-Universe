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
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is SoulContractRecord option && option.CompanionId == id);
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
    }

    [Fact]
    public async Task Both_choices_explain_shared_life_first_and_temporary_options_are_removed_after_selection()
    {
        var card = await AddToHand<SoulContract>();
        CardModel[] offered = [];
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            offered = options.ToArray();
            Assert.Equal(2, offered.Length);
            Assert.Equal(new[] { CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId },
                offered.Cast<SoulContractRecord>().Select(option => option.CompanionId).ToArray());
            foreach (var option in offered.Cast<SoulContractRecord>())
            {
                string name = CompanionSummons.GetName(option.CompanionId).GetFormattedText();
                Assert.StartsWith($"[gold]{name}[/gold]解锁[gold]共享生命[/gold]。\n",
                    option.GetDescriptionForPile(PileType.None));
                Assert.Contains("失去7点生命", option.GetDescriptionForPile(PileType.None));
            }
            return [offered[1]];
        });
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
        Assert.DoesNotContain(Player.PlayerCombatState!.AllCards,
            current => offered.Any(option => ReferenceEquals(option, current)));
        Assert.Equal(CompanionSummons.ThirdBrotherId,
            Assert.Single(CompanionSkillBook.Pile.GetPile(Player).Cards.OfType<SoulContractRecord>()).CompanionId);
    }

    [Theory]
    [InlineData(CompanionSummons.SecondBrotherId, false)]
    [InlineData(CompanionSummons.SecondBrotherId, true)]
    [InlineData(CompanionSummons.ThirdBrotherId, false)]
    [InlineData(CompanionSummons.ThirdBrotherId, true)]
    public async Task Choice_unlocks_a_skill_and_a_separate_contract_without_summoning_or_losing_hp(string id, bool upgraded)
    {
        int hp = Player.Creature.CurrentHp;
        await ChooseBrother(id, upgraded);
        Assert.Null(CompanionSummons.GetCompanion(Player, id));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        var records = CompanionSkillBook.Pile.GetPile(Player).Cards;
        var skill = Assert.Single(records.OfType<CompanionSkillRecord>());
        var contract = Assert.Single(records.OfType<SoulContractRecord>());
        Assert.Equal("SharedLife", skill.Skill);
        Assert.Equal(id, skill.CompanionId);
        Assert.Equal(id, contract.CompanionId);
        Assert.Contains("战斗结束时", skill.GetDescriptionForPile(CompanionSkillBook.Pile));
        Assert.DoesNotContain("召唤之", skill.GetDescriptionForPile(CompanionSkillBook.Pile));
        Assert.Contains("7点生命", contract.GetDescriptionForPile(CompanionSkillBook.Pile));
        Assert.DoesNotContain(contract.HoverTips, tip => tip is MegaCrit.Sts2.Core.HoverTips.HoverTip text && text.Title == "召唤物技能");
        Assert.True(CompanionSkillBook.CanActivate(Player, id));
    }

    [Fact]
    public async Task Starter_is_an_innate_power_without_exhaust_and_upgrade_reduces_cost_to_zero()
    {
        Assert.Single(Player.Deck.Cards.OfType<SoulContract>());
        var card = await AddToHand<SoulContract>();
        Assert.Equal(CardType.Power, card.Type);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Upgrade(card);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        Assert.DoesNotContain("你可以", ModelDb.Power<SharedLifePower>().Description.GetFormattedText());
    }

    [Theory]
    [InlineData(CompanionSummons.SecondBrotherId)]
    [InlineData(CompanionSummons.ThirdBrotherId)]
    public async Task Contract_summons_and_revives_for_seven_with_independent_contributions_and_healing(string id)
    {
        await ChooseBrother(id);
        int hp = Player.Creature.CurrentHp;
        for (int i = 1; i <= 2; i++)
        {
            Assert.True(await CompanionSkillBook.Activate(Choice(), Player, id));
            var pet = CompanionSummons.GetCompanion(Player, id)!;
            Assert.Equal(7, pet.CurrentHp);
            Assert.Equal(7, pet.MaxHp);
            Assert.Equal(hp - i * 7, Player.Creature.CurrentHp);
            Assert.Equal(i * 7, SharedLifeSystem.GetTotal(Player, id));
            Assert.Single(pet.Powers.OfType<CompanionSkillPower>());
            Assert.False(await CompanionSkillBook.Activate(Choice(), Player, id));
            if (i == 1) await CreatureCmd.Kill(pet, force: true);
        }
        var living = CompanionSummons.GetCompanion(Player, id)!;
        Assert.Contains("至多恢复14点", living.GetPower<SharedLifePower>()!.Description.GetFormattedText());
        await CreatureCmd.SetCurrentHp(living, 3);
        int before = Player.Creature.CurrentHp;
        await living.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        await living.GetPower<SharedLifePower>()!.AfterCombatEnd(null!);
        Assert.Equal(before + 3, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Recall_is_independent_of_shared_life_skill_activation_and_does_not_consume_a_skill_slot()
    {
        var pet = (await CompanionSummons.SummonSpecific(Choice(), Player, CompanionSummons.SecondBrotherId, 6, null)).Creature!;
        await CompanionSkills.UnlockForCompanion<SpareTheYoungAndOldPower>(Choice(), Player, CompanionSummons.SecondBrotherId);
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        Assert.Equal(6, pet.MaxHp);
        Assert.Equal(10, pet.GetPower<SharedLifePower>()!.RequiredMaxHp);
        Assert.False(pet.GetPower<SharedLifePower>()!.IsActive);
        await CreatureCmd.Kill(pet, force: true);
        Assert.True(await CompanionSkillBook.Activate(Choice(), Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(7, pet.CurrentHp);
        Assert.False(pet.GetPower<SharedLifePower>()!.IsActive);
        Assert.Equal(2, pet.Powers.OfType<CompanionSkillPower>().Count());
    }

    [Fact]
    public async Task Contracts_are_unique_and_are_scoped_to_the_chosen_brother_and_player()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        Assert.Equal(2, CompanionSkillBook.Pile.GetPile(Player).Cards.Count);
        Assert.False(CompanionSkillBook.CanActivate(Player, CompanionSummons.ThirdBrotherId));
        var other = Players.Single(player => player != Player);
        Assert.Empty(CompanionSkillBook.Pile.GetPile(other).Cards);
        Assert.False(CompanionSkillBook.CanActivate(other, CompanionSummons.SecondBrotherId));
        await ChooseBrother(CompanionSummons.ThirdBrotherId);
        Assert.Equal(4, CompanionSkillBook.Pile.GetPile(Player).Cards.Count);
        Assert.True(CompanionSkillBook.CanActivate(Player, CompanionSummons.ThirdBrotherId));
    }

    [Fact]
    public async Task Buffer_and_block_do_not_inflate_the_healing_cap()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        await CreatureCmd.GainBlock(Player.Creature, 15, ValueProp.Unpowered, null);
        await PowerCmd.Apply<BufferPower>(Choice(), Player.Creature, 1, Player.Creature, null);
        int hp = Player.Creature.CurrentHp;
        Assert.True(await CompanionSkillBook.Activate(Choice(), Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(15, Player.Creature.Block);
        Assert.Equal(0, SharedLifeSystem.GetTotal(Player));
    }

    [Fact]
    public async Task Unstable_factor_losses_still_count_for_third_brothers_healing_after_unlock()
    {
        await Play(await AddToHand<UnstableFactor>());
        await Player.Creature.GetPower<UnstableFactorPower>()!.AfterSideTurnStart(MegaCrit.Sts2.Core.Combat.CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(2, SharedLifeSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        var third = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        await ChooseBrother(CompanionSummons.ThirdBrotherId);
        Assert.Equal(2, third.GetPower<SharedLifePower>()!.DisplayAmount);
        Assert.Equal(4, third.MaxHp);
        Assert.Equal("[gold]召唤物技能[/gold]：战斗结束时，为你恢复等量于[gold]三弟[/gold]的生命（至多恢复2点，此上限为你为[gold]三弟[/gold]失去过的生命）。",
            third.GetPower<SharedLifePower>()!.Description.GetFormattedText());
    }
}
