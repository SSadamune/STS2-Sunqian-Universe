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

public sealed class CompanionSkillBookTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("companion-skill-collection");
    private static BlockingPlayerChoiceContext Choice() => new();
    private async Task ChooseBrother(string id)
    {
        var card = await AddToHand<SoulContract>();
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is SoulContractRecord option && option.CompanionId == id);
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
    }

    [Fact]
    public async Task Collection_cancel_has_no_effect_and_confirmed_selection_runs_the_managed_action()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        int hp = Player.Creature.CurrentHp;
        var cancel = new ScriptedCardSelector().ThenSkip();
        using (UseCardSelector(cancel)) await CompanionSkillBook.Open(Player);
        AssertAllSelectionsConsumed(cancel);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        var confirm = new ScriptedCardSelector().ThenSelectWhere(card => card is SoulContractRecord);
        using (UseCardSelector(confirm)) await CompanionSkillBook.Open(Player);
        AssertAllSelectionsConsumed(confirm);
        await WaitForIdle();
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        Assert.True(pet.IsAlive);
        Assert.Equal(7, pet.CurrentHp);
        Assert.Equal(hp - 7, Player.Creature.CurrentHp);
        Assert.Equal(7, SharedLifeSystem.GetTotal(Player));
    }

}
