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
    public async Task Upgraded_power_transforms_into_an_upgraded_script_without_drawing()
    {
        await Play(Upgrade(await AddToHand<DistributedScreenwriter>()));
        var selected = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var power = Assert.IsType<DistributedScreenwriterPower>(
            Player.Creature.GetPower<DistributedScreenwriterPower>());
        int scriptCount = PileType.Hand.GetPile(Player).Cards.Count(card =>
            card.Tags.Contains(SquCardTags.Script));
        var selector = new ScriptedCardSelector().ThenSelect(selected);
        int drawCount = PileType.Draw.GetPile(Player).Cards.Count;
        var previousHand = PileType.Hand.GetPile(Player).Cards.ToHashSet();

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
        Assert.Equal(drawCount, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Equal(1, power.DynamicVars[DistributedScreenwriterPower.UpgradedCardsVarName].IntValue);
        var transformed = Assert.Single(PileType.Hand.GetPile(Player).Cards.Where(card => !previousHand.Contains(card)));
        Assert.True(transformed.IsUpgraded);
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
        Assert.Equal(0, power.DynamicVars[DistributedScreenwriterPower.UpgradedCardsVarName].IntValue);
        Assert.DoesNotContain(PileType.Hand.GetPile(Player).Cards, card => ReferenceEquals(card, selected));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task Stacked_copies_preserve_the_number_of_upgraded_transformations(bool secondUpgraded, int upgrades)
    {
        await MegaCrit.Sts2.Core.Commands.PlayerCmd.SetEnergy(10, Player);
        await Play(Upgrade(await AddToHand<DistributedScreenwriter>()));
        var secondPower = await AddToHand<DistributedScreenwriter>();
        if (secondUpgraded) Upgrade(secondPower);
        await Play(secondPower);
        var first = await AddToHand<StrikeIronclad>();
        var second = await AddToHand<DefendIronclad>();
        var before = PileType.Hand.GetPile(Player).Cards.ToHashSet();
        int drawCount = PileType.Draw.GetPile(Player).Cards.Count;
        var selector = new ScriptedCardSelector().ThenSelect(first, second);
        var power = Player.Creature.GetPower<DistributedScreenwriterPower>()!;
        using (UseCardSelector(selector))
        {
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        }
        AssertAllSelectionsConsumed(selector);
        var generated = PileType.Hand.GetPile(Player).Cards.Where(card => !before.Contains(card)).ToArray();
        Assert.Equal(2, generated.Length);
        Assert.All(generated, card => Assert.Contains(SquCardTags.Script, card.Tags));
        Assert.Equal(upgrades, generated.Count(card => card.IsUpgraded));
        Assert.Equal(drawCount, PileType.Draw.GetPile(Player).Cards.Count);
    }
}
