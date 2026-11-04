using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SharedLifeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("life-contract-cap");

    [Theory]
    [InlineData(CompanionSummons.SecondBrotherId, 3, 8, 3)]
    [InlineData(CompanionSummons.SecondBrotherId, 8, 8, 8)]
    [InlineData(CompanionSummons.SecondBrotherId, 12, 8, 8)]
    [InlineData(CompanionSummons.ThirdBrotherId, 3, 8, 3)]
    [InlineData(CompanionSummons.ThirdBrotherId, 8, 8, 8)]
    [InlineData(CompanionSummons.ThirdBrotherId, 12, 8, 8)]
    [InlineData(CompanionSummons.ThirdBrotherId, 12, 0, 0)]
    public async Task Healing_uses_remaining_hp_capped_by_contributions_and_claims_once(
        string id, int hp, int cap, int expected)
    {
        var context = new BlockingPlayerChoiceContext();
        var pet = (await CompanionSummons.SummonSpecific(context, Player, id, 20, null)).Creature!;
        await CreatureCmd.SetCurrentHp(pet, hp);
        await CreatureCmd.SetCurrentHp(Player.Creature, 20);
        SharedLifeSystem.AddContribution(Player, cap, id);
        await CompanionSkills.UnlockForCompanion<SharedLifePower>(context, Player, id);
        var power = pet.GetPower<SharedLifePower>()!;
        Assert.Equal(cap, power.DisplayAmount);
        Assert.Contains($"至多恢复{cap}点，此上限为", power.Description.GetFormattedText());
        Assert.Contains(CompanionSummons.GetName(id).GetFormattedText(), power.Description.GetFormattedText());
        await power.AfterCombatEnd(null!);
        Assert.Equal(20 + expected, Player.Creature.CurrentHp);
        await power.AfterCombatEnd(null!);
        Assert.Equal(20 + expected, Player.Creature.CurrentHp);
    }
}
