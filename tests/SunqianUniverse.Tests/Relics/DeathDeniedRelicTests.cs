using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class DeathDeniedRelicTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>()
            .WithSeed("death-denied-relic");

    [Fact]
    public async Task Combat_description_shows_current_recorded_hp_loss_and_updates_with_damage()
    {
        var relic = await AddRelic();
        Assert.EndsWith(".combatDescription", relic.DynamicDescription.LocEntryKey);
        Assert.Contains("0", relic.DynamicDescription.GetFormattedText().Split('\n').Last());
        ((CombatState)Combat).CurrentSide = CombatSide.Player;
        await DamagePlayer(13);
        Assert.Contains("13", relic.DynamicDescription.GetFormattedText().Split('\n').Last());
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        await DamagePlayer(7);
        Assert.Contains("13", relic.DynamicDescription.GetFormattedText().Split('\n').Last());
        Assert.EndsWith(".description", ModelDb.Relic<DeathDeniedRelic>().DynamicDescription.LocEntryKey);
    }

    [Fact]
    public async Task Victory_heals_half_of_only_the_hp_lost_during_player_turns()
    {
        DeathDeniedRelic relic = await AddRelic();
        int startingHp = Player.Creature.CurrentHp;

        ((CombatState)Combat).CurrentSide = CombatSide.Player;
        await DamagePlayer(10);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        await DamagePlayer(6);

        Assert.Equal(10, relic.HpLostThisCombat);
        Assert.Equal(5, relic.DisplayAmount);

        await relic.AfterCombatVictoryEarly(null!);

        Assert.Equal(startingHp - 11, Player.Creature.CurrentHp);
        Assert.Equal(0, relic.HpLostThisCombat);
    }

    [Fact]
    public async Task Lethal_damage_activates_immediately_then_disables_for_the_combat()
    {
        DeathDeniedRelic relic = await AddRelic();

        ((CombatState)Combat).CurrentSide = CombatSide.Player;
        await DamagePlayer(20);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        await DamagePlayer(Player.Creature.CurrentHp);

        Assert.Equal(10, Player.Creature.CurrentHp);
        Assert.True(relic.DisabledThisCombat);
        Assert.True(relic.IsUsedUp);
        Assert.True(relic.ShouldDieLate(Player.Creature));

        int hp = Player.Creature.CurrentHp;
        await relic.AfterCombatEnd(null!);
        Assert.True(relic.DisabledThisCombat);
        await relic.AfterCombatVictoryEarly(null!);
        Assert.Equal(hp, Player.Creature.CurrentHp);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task End_hooks_preserve_recovery_until_victory_with_or_without_chosen_cheese(bool withCheese)
    {
        var relic = await AddRelic();
        if (withCheese)
            Player.AddRelicInternal(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.ChosenCheese>().ToMutable(), silent: true);
        ((CombatState)Combat).CurrentSide = CombatSide.Player;
        await DamagePlayer(20);
        int before = Player.Creature.CurrentHp;
        // CombatManager dispatches AfterCombatEnd before AfterCombatVictoryEarly.
        await relic.AfterCombatEnd(null!);
        if (withCheese)
            await Player.Relics.OfType<MegaCrit.Sts2.Core.Models.Relics.ChosenCheese>().Single().AfterCombatEnd(null!);
        Assert.Equal(20, relic.HpLostThisCombat);
        int afterEnd = Player.Creature.CurrentHp;
        Assert.Equal(before + (withCheese ? 1 : 0), afterEnd);
        await relic.AfterCombatVictoryEarly(null!);
        Assert.Equal(afterEnd + 10, Player.Creature.CurrentHp);
        Assert.Equal(0, relic.HpLostThisCombat);
        await relic.BeforeCombatStart();
        Assert.False(relic.DisabledThisCombat);
    }

    private Task DamagePlayer(decimal amount) =>
        CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Player.Creature,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

    private async Task<DeathDeniedRelic> AddRelic()
    {
        var relic = (DeathDeniedRelic)ModelDb.Relic<DeathDeniedRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await relic.BeforeCombatStart();
        return relic;
    }
}
