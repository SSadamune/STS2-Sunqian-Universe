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

public sealed class CompanionSkillCollectionTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("companion-skill-collection");
    private static BlockingPlayerChoiceContext Choice() => new();
    private async Task ChooseBrother(string id)
    {
        var card = await AddToHand<SoulContract>();
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is CompanionSummonChoice option && option.CompanionId == id);
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
    }

    [Fact]
    public async Task Collection_cancel_has_no_effect_and_confirmed_selection_runs_the_managed_action()
    {
        await ChooseBrother(CompanionSummons.SecondBrotherId);
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        await CreatureCmd.Kill(pet, force: true);
        int hp = Player.Creature.CurrentHp;
        var cancel = new ScriptedCardSelector().ThenSkip();
        using (UseCardSelector(cancel)) await CompanionSkillCollection.Open(Player);
        AssertAllSelectionsConsumed(cancel);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.False(pet.IsAlive);
        var confirm = new ScriptedCardSelector().ThenSelectFirst();
        using (UseCardSelector(confirm)) await CompanionSkillCollection.Open(Player);
        AssertAllSelectionsConsumed(confirm);
        await WaitForIdle();
        Assert.True(pet.IsAlive);
        Assert.Equal(8, pet.CurrentHp);
        Assert.Equal(hp - 8, Player.Creature.CurrentHp);
        Assert.Equal(8, SharedLifeSystem.GetTotal(Player));
    }

}
