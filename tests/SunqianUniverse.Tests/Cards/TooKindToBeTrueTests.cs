using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TooKindToBeTrueTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("too-kind-to-be-true");

    [Fact]
    public async Task No_transformable_attack_skips_selection_but_still_applies_the_power()
    {
        foreach (var existing in PileType.Hand.GetPile(Player).Cards.ToArray())
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(existing, PileType.Discard);
        }

        await Play(await AddToHand<TooKindToBeTrue>());

        CardTestAssertions.AssertPowerAmount<TooKindToBeTruePower>(
            Player.Creature,
            TooKindToBeTrue.ExtraBlockPerDebuff);
    }

    [Fact]
    public async Task Play_grants_upgraded_ultimate_defend_revision_to_selected_attack()
    {
        var attack = await AddToHand<StrikeIronclad>();
        var selector = new ScriptedCardSelector().ThenSelect(attack);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<TooKindToBeTrue>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Contains(SquKeywords.SlightRevision, attack.Keywords);
        Assert.True(SlightRevisionSystem.TryGetRevision(attack, out var target, out bool upgraded));
        Assert.IsType<UltimateDefend>(target);
        Assert.True(upgraded);
        CardTestAssertions.AssertPowerAmount<TooKindToBeTruePower>(
            Player.Creature,
            TooKindToBeTrue.UpgradedExtraBlockPerDebuff);
    }

    [Fact]
    public async Task Distinct_debuff_types_each_add_upgraded_block_to_defend_cards()
    {
        var card = Upgrade(await AddToHand<TooKindToBeTrue>());
        await ApplyPower<TooKindToBeTruePower>(
            Player.Creature,
            TooKindToBeTrue.UpgradedExtraBlockPerDebuff,
            Player.Creature,
            card);
        await ApplyPower<WeakPower>(Player.Creature, 1);
        await ApplyPower<VulnerablePower>(Player.Creature, 1);

        await Play(await AddToHand<DefendLongtao>());

        Assert.Equal(DefendLongtao.BaseBlock + TooKindToBeTrue.UpgradedExtraBlockPerDebuff * 2, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<TooKindToBeTruePower>(Player.Creature, TooKindToBeTrue.UpgradedExtraBlockPerDebuff);
    }
}
