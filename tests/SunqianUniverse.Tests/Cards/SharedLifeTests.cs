using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
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

    [Fact]
    public async Task Shared_life_power_and_card_hovers_start_with_the_companion_skill_prefix()
    {
        var context = new BlockingPlayerChoiceContext();
        string id = CompanionSummons.SecondBrotherId;
        await CompanionSummons.SummonSpecific(context, Player, id, 20, null);
        SharedLifeSystem.AddContribution(Player, 5, id);
        await CompanionSkills.UnlockForCompanion<SharedLifePower>(context, Player, id);
        var power = CompanionSummons.GetCompanion(Player, id)!.GetPower<SharedLifePower>()!;

        Assert.StartsWith("[gold]召唤物技能[/gold]：", power.Description.GetFormattedText());
        HoverTip powerTip = Assert.Single(power.HoverTips.OfType<HoverTip>(), tip => tip.Title == "共享生命");
        Assert.StartsWith("[gold]召唤物技能[/gold]：", powerTip.Description);

        var contract = await AddToHand<SoulContract>();
        HoverTip contractTip = Assert.Single(contract.HoverTips.OfType<HoverTip>(), tip => tip.Title == "共享生命");
        Assert.StartsWith("[gold]召唤物技能[/gold]：", contractTip.Description);
    }

    [Theory]
    [InlineData("zhs", "[gold]召唤物技能[/gold]：")]
    [InlineData("eng", "[gold]Companion Skill[/gold]: ")]
    public void Shared_life_localization_prefix_is_consistent_in_both_languages(string language, string prefix)
    {
        using JsonDocument table = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            $"res://sunqian-universe/localization/{language}/powers.json"));
        foreach (string key in new[]
                 {
                     "SUNQIAN_UNIVERSE_POWER_SHARED_LIFE_POWER.description",
                     "SUNQIAN_UNIVERSE_POWER_SHARED_LIFE_POWER.unlockedDescription",
                 })
        {
            string text = table.RootElement.GetProperty(key).GetString()!;
            Assert.StartsWith(prefix, text);
        }
    }
}
