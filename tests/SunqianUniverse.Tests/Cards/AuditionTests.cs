using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class AuditionTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<SunqianCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-audition");
    }

    [Fact]
    public async Task Audition_generates_three_distinct_script_cards()
    {
        var audition = await AddToHand<Audition>();
        HashSet<CardModel> handBefore = PileType.Hand.GetPile(Player).Cards.ToHashSet();

        await Play(audition);

        CardModel[] generated = PileType.Hand.GetPile(Player).Cards
            .Where(card => !handBefore.Contains(card))
            .ToArray();
        Assert.Equal(Audition.ScriptCardCount, generated.Length);
        Assert.All(generated, card => Assert.Contains(SquCardTags.Script, card.Tags));
        Assert.Equal(
            Audition.ScriptCardCount,
            generated.Select(card => card.GetType()).Distinct().Count());
    }
}
