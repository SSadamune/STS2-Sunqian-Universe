using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HumanTransmutationTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("human-transmutation");

    [Fact]
    public async Task Upgrade_exhausts_three_rarities_and_adds_the_selected_upgraded_enchanted_attack()
    {
        var basic = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var uncommon = await AddCardToPile<FlameStrike>(PileType.Hand);
        var rare = await AddCardToPile<AllIn>(PileType.Hand);
        var fourth = await AddCardToPile<DefendIronclad>(PileType.Hand);
        CardModel? generated = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(basic, uncommon, rare)
            .ThenSelect(options =>
            {
                generated = options.Single(card => card.Rarity == CardRarity.Rare);
                return [generated];
            });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<HumanTransmutation>()));
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(basic, PileType.Exhaust);
        AssertInPile(uncommon, PileType.Exhaust);
        AssertInPile(rare, PileType.Exhaust);
        AssertInPile(fourth, PileType.Hand);
        Assert.NotNull(generated);
        Assert.True(generated.IsUpgraded);
        Assert.NotNull(generated.Enchantment);
        AssertInPile(generated, PileType.Hand);
    }

    [Fact]
    public async Task Skipping_optional_exhaust_ends_without_generation_prompt_or_generated_card()
    {
        var retained = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        int handCount = PileType.Hand.GetPile(Player).Cards.Count;
        var selector = new ScriptedCardSelector().ThenSkip();

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<HumanTransmutation>());
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(retained, PileType.Hand);
        Assert.Equal(handCount, PileType.Hand.GetPile(Player).Cards.Count);
    }
}
