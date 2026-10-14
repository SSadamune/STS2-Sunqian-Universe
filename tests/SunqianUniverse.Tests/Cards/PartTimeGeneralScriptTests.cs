using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PartTimeGeneralScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("part-time-general");

    [Fact]
    public async Task Expiring_script_offers_recorded_strikes_and_adds_the_selected_copy_to_hand()
    {
        int hp = EnemyAt(0).CurrentHp;
        await Play(Upgrade(await AddToHand<PartTimeGeneralScript>()), EnemyAt(0));
        Assert.True(EnemyAt(0).CurrentHp < hp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.False(string.IsNullOrWhiteSpace(
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString()));
        await PlayerCmd.SetEnergy(3, Player);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            selected = options[0];
            return [selected];
        });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<ServantScript>());
        }

        AssertAllSelectionsConsumed(selector);
        CardModel chosen = Assert.IsAssignableFrom<CardModel>(selected);
        AssertInPile(chosen, PileType.Hand);
        Assert.True(chosen.IsUpgraded);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Zero_energy_plays_no_strikes_but_still_installs_an_empty_script()
    {
        await PlayerCmd.SetEnergy(0, Player);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<PartTimeGeneralScript>(), EnemyAt(0));

        Assert.Equal(hp, EnemyAt(0).CurrentHp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.True(string.IsNullOrWhiteSpace(
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString()));

        await PlayerCmd.SetEnergy(2, Player);
        await Play(await AddToHand<ServantScript>());

        AssertOnlyScript<ScriptServantPower>();
    }
}
