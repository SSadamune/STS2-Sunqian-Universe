using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LoyaltyOverKinTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("loyalty-over-kin");

    [Fact]
    public async Task Upgrade_exhausts_both_available_draw_cards_hits_three_times_and_gains_strength()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        var first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var second = await AddCardToPile<DefendIronclad>(PileType.Draw);
        int hp = EnemyAt(0).CurrentHp;

        await Play(Upgrade(await AddToHand<LoyaltyOverKin>()), EnemyAt(0));

        AssertInPile(first, PileType.Exhaust);
        AssertInPile(second, PileType.Exhaust);
        int expectedDamage = LoyaltyOverKin.DamageAmount
            + (LoyaltyOverKin.DamageAmount + LoyaltyOverKin.UpgradedStrength)
            + (LoyaltyOverKin.DamageAmount + LoyaltyOverKin.UpgradedStrength * 2);
        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(
            Player.Creature,
            LoyaltyOverKin.UpgradedStrength * 2);
    }

    [Fact]
    public async Task Empty_draw_pile_performs_only_the_initial_attack_and_grants_no_strength()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<LoyaltyOverKin>(), EnemyAt(0));

        Assert.Equal(hp - LoyaltyOverKin.DamageAmount, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<StrengthPower>());
    }

    [Fact]
    public async Task Dynamic_target_preview_uses_text_only_without_nested_card_nodes()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        StrikeIronclad first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        DefendIronclad second = await AddCardToPile<DefendIronclad>(PileType.Draw);
        LoyaltyOverKin card = await AddToHand<LoyaltyOverKin>();

        IHoverTip[] tips = card.HoverTips.ToArray();

        Assert.Empty(tips.OfType<CardHoverTip>());
        Assert.Contains(tips.OfType<HoverTip>(), tip =>
            tip.Description?.Contains(first.Title, StringComparison.Ordinal) == true
            && tip.Description.Contains(second.Title, StringComparison.Ordinal));
    }
}
