using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class DistributedScreenwriterTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddEnemy<BigDummy>().WithSeed("distributed-screenwriter");

    [Fact]
    public async Task Upgraded_power_draws_then_transforms_the_selected_hand_card_into_a_script()
    {
        await Play(Upgrade(await AddToHand<DistributedScreenwriter>()));
        var selected = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var power = Assert.IsType<DistributedScreenwriterPower>(
            Player.Creature.GetPower<DistributedScreenwriterPower>());
        int scriptCount = PileType.Hand.GetPile(Player).Cards.Count(card =>
            card.Tags.Contains(SquCardTags.Script));
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.DoesNotContain(
            PileType.Hand.GetPile(Player).Cards,
            card => ReferenceEquals(card, selected));
        Assert.Equal(
            scriptCount + 1,
            PileType.Hand.GetPile(Player).Cards.Count(card => card.Tags.Contains(SquCardTags.Script)));
        Assert.Equal(1, power.DynamicVars[DistributedScreenwriterPower.DrawCardsVarName].IntValue);
    }

    [Fact]
    public async Task Normal_power_transforms_without_drawing_first()
    {
        await Play(await AddToHand<DistributedScreenwriter>());
        var selected = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var power = Assert.IsType<DistributedScreenwriterPower>(
            Player.Creature.GetPower<DistributedScreenwriterPower>());
        int drawCount = PileType.Draw.GetPile(Player).Cards.Count;
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(drawCount, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Equal(0, power.DynamicVars[DistributedScreenwriterPower.DrawCardsVarName].IntValue);
        Assert.DoesNotContain(PileType.Hand.GetPile(Player).Cards, card => ReferenceEquals(card, selected));
    }
}
