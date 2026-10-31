using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

public sealed class PartTimeGeneralHeadsmanTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("part-time-general-headsman");

    [Fact]
    public async Task Nested_strike_kill_restores_headsman_resources_once_after_the_parent_replaces_it()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await PlayAndActivateScript(await AddToHand<HeadsmanScript>(), EnemyAt(1));
        Creature victim = EnemyAt(0);
        await CreatureCmd.SetCurrentHp(victim, 1);
        await PlayerCmd.SetEnergy(1, Player);

        MethodInfo chooseStrikes = AccessTools.DeclaredMethod(
                typeof(PartTimeGeneralScript),
                "ChooseStrikeCards")
            ?? throw new InvalidOperationException("ChooseStrikeCards was not found.");
        var harmony = new Harmony("squ.tests.headsman-part-time-general-kill");
        harmony.Patch(
            chooseStrikes,
            prefix: new HarmonyMethod(typeof(PartTimeGeneralHeadsmanTests), nameof(ForcePommelStrike)));
        try
        {
            await PlayAndActivateScript(
                await AddToHand<PartTimeGeneralScript>(),
                victim);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        Assert.True(victim.IsDead);
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        Assert.Equal(
            HeadsmanScript.BaseVigor,
            (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0m));
        AssertOnlyScript<ScriptPartTimeGeneralPower>();
    }

    private static bool ForcePommelStrike(int count, ref List<CardModel> __result)
    {
        __result = count <= 0 ? [] : [ModelDb.Card<PommelStrike>()];
        return false;
    }
}
