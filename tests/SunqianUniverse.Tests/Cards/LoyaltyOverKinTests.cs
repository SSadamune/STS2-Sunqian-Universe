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

        LoyaltyOverKin card = await AddToHand<LoyaltyOverKin>();
        Assert.Empty(card.HoverTips.OfType<CardHoverTip>());
        Assert.Contains(card.HoverTips.OfType<HoverTip>(), tip =>
            tip.Id.EndsWith(".targetCardsNone", StringComparison.Ordinal));

        await Play(card, EnemyAt(0));

        Assert.Equal(hp - LoyaltyOverKin.DamageAmount, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<StrengthPower>());
    }

    [Fact]
    public async Task Higher_rate_exhaust_still_pays_out_for_celebrate_mourning_pulled_to_hand()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        CutePet higherRate = await AddCardToPile<CutePet>(PileType.Draw);
        CelebrateMourning celebrate = await AddCardToPile<CelebrateMourning>(PileType.Draw);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<LoyaltyOverKin>(), EnemyAt(0));

        AssertInPile(higherRate, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
        int expectedDamage = LoyaltyOverKin.DamageAmount
            + (LoyaltyOverKin.DamageAmount + LoyaltyOverKin.BaseStrength)
            + (LoyaltyOverKin.DamageAmount + LoyaltyOverKin.BaseStrength * 2);
        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(
            Player.Creature,
            LoyaltyOverKin.BaseStrength * 2);
    }

    [Fact]
    public async Task Combat_preview_shows_each_exhaust_target_card_face()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        StrikeIronclad first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        StrikeIronclad second = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        LoyaltyOverKin card = await AddToHand<LoyaltyOverKin>();

        IHoverTip[] tips = card.HoverTips.ToArray();
        CardHoverTip[] faces = tips.OfType<CardHoverTip>().ToArray();

        Assert.Equal(2, faces.Length);
        Assert.Contains(faces, tip => ReferenceEquals(tip.Card, first));
        Assert.Contains(faces, tip => ReferenceEquals(tip.Card, second));
        Assert.Contains(tips.OfType<HoverTip>(), tip =>
            tip.Id.EndsWith(".targetCards", StringComparison.Ordinal)
            && tip.Description?.Contains(first.Title, StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Combat_preview_warns_when_only_one_card_can_be_exhausted()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
        StrikeIronclad only = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        LoyaltyOverKin card = await AddToHand<LoyaltyOverKin>();

        IHoverTip[] tips = card.HoverTips.ToArray();
        CardHoverTip face = Assert.Single(tips.OfType<CardHoverTip>());

        Assert.Same(only, face.Card);
        Assert.Contains(tips.OfType<HoverTip>(), tip =>
            tip.Id.EndsWith(".targetCardsInsufficient", StringComparison.Ordinal)
            && tip.Description?.Contains(only.Title, StringComparison.Ordinal) == true);
    }
}
