using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
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
        await PlayAndActivateScript(
            Upgrade(await AddToHand<PartTimeGeneralScript>()),
            EnemyAt(0));
        Assert.True(EnemyAt(0).CurrentHp < hp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.False(string.IsNullOrWhiteSpace(
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString()));
        await PlayerCmd.SetEnergy(3, Player);
        CardModel? selected = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
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

        await PlayAndActivateScript(
            await AddToHand<PartTimeGeneralScript>(),
            EnemyAt(0));

        Assert.Equal(hp, EnemyAt(0).CurrentHp);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.True(string.IsNullOrWhiteSpace(
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString()));

        await PlayerCmd.SetEnergy(2, Player);
        await PlayAndActivateScript(await AddToHand<ServantScript>());

        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Nested_strike_drawn_by_pommel_and_autoplayed_by_hellraiser_keeps_supreme_general_vigor()
    {
        const int vigor = 5;
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        await ApplyPower<HellraiserPower>(Player.Creature, 1);
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        StrikeIronclad drawnStrike = await AddToDrawPileTop<StrikeIronclad>();
        await PlayerCmd.SetEnergy(1, Player);
        int hp = EnemyAt(0).CurrentHp;
        int expectedDamage = (int)(
            ModelDb.Card<PommelStrike>().DynamicVars.Damage.BaseValue
            + drawnStrike.DynamicVars.Damage.BaseValue
            + vigor * 2);

        MethodInfo chooseStrikes = AccessTools.DeclaredMethod(
                typeof(PartTimeGeneralScript),
                "ChooseStrikeCards")
            ?? throw new InvalidOperationException("ChooseStrikeCards was not found.");
        var harmony = new Harmony("squ.tests.force-pommel-strike");
        harmony.Patch(
            chooseStrikes,
            prefix: new HarmonyMethod(typeof(PartTimeGeneralScriptTests), nameof(ForcePommelStrike)));
        try
        {
            await PlayAndActivateScript(
                await AddToHand<PartTimeGeneralScript>(),
                EnemyAt(0));
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        Assert.True((Player.Creature.GetPower<VigorPower>()?.Amount ?? 0m) == 0m);
        AssertInPile(drawnStrike, PileType.Discard);
        var script = Assert.IsType<ScriptPartTimeGeneralPower>(
            Player.Creature.GetPower<ScriptPartTimeGeneralPower>());
        Assert.Contains(
            ModelDb.Card<PommelStrike>().Title,
            script.DynamicVars[ScriptPartTimeGeneralPower.RecordedCardsVarName].ToString());
    }

    private static bool ForcePommelStrike(int count, ref List<CardModel> __result)
    {
        __result = count <= 0 ? [] : [ModelDb.Card<PommelStrike>()];
        return false;
    }
}
