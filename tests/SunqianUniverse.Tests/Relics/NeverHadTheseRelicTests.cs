using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class NeverHadTheseRelicTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("never-had-these-relic");

    [Fact]
    public async Task Counter_and_effect_counts_track_only_family_cards_removed_from_its_owners_deck()
    {
        NeverHadTheseRelic relic = CreateRelic();
        CardModel family = Player.RunState.CreateCard<CutePet>(Player);
        CardModel ordinary = Player.RunState.CreateCard<StrikeIronclad>(Player);

        Assert.False(relic.ShowCounter);
        Assert.Equal(0, relic.DisplayAmount);

        await relic.BeforeCardRemoved(ordinary);
        Assert.Equal(0, relic.RemovedFamilyCards);

        await relic.BeforeCardRemoved(family);
        Assert.Equal(1, relic.RemovedFamilyCards);
        Assert.Equal(1, relic.RemovedCutePets);
        Assert.Equal(0, relic.RemovedBelovedPartners);
        Assert.Equal(0, relic.RemovedNextOfKin);
        Assert.True(relic.ShowCounter);
        Assert.Equal(1, relic.DisplayAmount);
    }

    [Fact]
    public async Task First_player_turn_grants_energy_and_triggers_each_removed_family_effect()
    {
        NeverHadTheseRelic relic = CreateRelic();
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<CutePet>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<BelovedPartner>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<BelovedPartner>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<NextOfKin>(Player));
        int energyBefore = Player.PlayerCombatState!.Energy;
        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;
        int petsBefore = Combat.Creatures.Count(creature => creature.IsPet);

        await relic.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Combat);

        Assert.Equal(energyBefore + 4, Player.PlayerCombatState.Energy);
        Assert.Equal(drawBefore - NextOfKin.DrawAmount, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Equal(petsBefore + 1, Combat.Creatures.Count(creature => creature.IsPet));
        CardTestAssertions.AssertPowerAmount<RegenPower>(
            Player.Creature,
            BelovedPartner.RegenAmount * 2);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BelovedPartner.TinderAmount * 2);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());
    }

    [Fact]
    public async Task Zero_counter_or_a_turn_without_the_owner_does_not_grant_rewards()
    {
        NeverHadTheseRelic relic = CreateRelic();
        int energyBefore = Player.PlayerCombatState!.Energy;

        await relic.AfterSideTurnStart(CombatSide.Player, [], Combat);
        Assert.Equal(energyBefore, Player.PlayerCombatState.Energy);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());

        relic.RemovedFamilyCards = 2;
        await relic.AfterSideTurnStart(CombatSide.Enemy, [EnemyAt(0)], Combat);
        Assert.Equal(energyBefore, Player.PlayerCombatState.Energy);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());
    }

    [Fact]
    public void Canonical_hover_tip_explains_energy_without_showing_run_specific_effects()
    {
        IHoverTip[] tips = ModelDb.Relic<NeverHadTheseRelic>().HoverTips.ToArray();

        Assert.Contains(tips, tip => tip.CanonicalModel is NeverHadTheseRelic);
        Assert.Contains(
            tips.OfType<HoverTip>(),
            tip => tip.Id.Contains("ENERGY.title", StringComparison.Ordinal));
        Assert.DoesNotContain(
            tips.OfType<HoverTip>(),
            tip => tip.Id == ModelDb.Power<DexterityPower>().Id.ToString());

        string description = FormatDescription(ModelDb.Relic<NeverHadTheseRelic>());
        Assert.DoesNotContain("会触发", description);
        Assert.DoesNotContain("召唤[/gold]4", description);
    }

    [Fact]
    public async Task Owned_relic_description_lists_each_recorded_effect_once_inside_the_run()
    {
        NeverHadTheseRelic relic = CreateRelic();
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<CutePet>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<CutePet>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<BelovedPartner>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<BelovedPartner>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<NextOfKin>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<NextOfKin>(Player));
        await relic.BeforeCardRemoved(Player.RunState.CreateCard<NextOfKin>(Player));

        string description = FormatDescription(relic);

        Assert.Contains("会触发", description);
        Assert.Equal(1, description.Split("召唤[/gold]8").Length - 1);
        Assert.Contains("获得4层[gold]再生[/gold]和6层[gold]火种[/gold]", description);
        Assert.Contains("抽6张牌", description);
    }

    private NeverHadTheseRelic CreateRelic()
    {
        var relic = (NeverHadTheseRelic)ModelDb.Relic<NeverHadTheseRelic>().ToMutable();
        relic.Owner = Player;
        return relic;
    }

    private static string FormatDescription(RelicModel relic)
    {
        var description = relic.DynamicDescription;
        relic.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}
