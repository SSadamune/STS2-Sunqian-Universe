using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MetamorphosisTests : SunqianCombatTestSuite
{
    private sealed class FixedProvider(IReadOnlyList<MetamorphosisCandidate> choices)
        : IMetamorphosisCandidateProvider
    {
        public IReadOnlyList<MetamorphosisCandidate> BuildChoices(Squ.Cards.Metamorphosis source, int count) =>
            choices.Take(count).ToArray();
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("metamorphosis");

    [Fact]
    public async Task Upgrade_adds_selected_upgraded_card_and_records_its_character_pool()
    {
        var silentPool = ModelDb.CardPool<SilentCardPool>();
        CardModel selected = Combat.CreateCard<Neutralize>(Player);
        var candidates = new[]
        {
            new MetamorphosisCandidate(selected, silentPool),
            new MetamorphosisCandidate(Combat.CreateCard<Backflip>(Player), silentPool),
            new MetamorphosisCandidate(Combat.CreateCard<Prepared>(Player), silentPool),
        };
        IMetamorphosisCandidateProvider original = Squ.Cards.Metamorphosis.CandidateProvider;
        Squ.Cards.Metamorphosis.CandidateProvider = new FixedProvider(candidates);
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        try
        {
            using (UseCardSelector(selector))
            {
                await Play(Upgrade(await AddToHand<Squ.Cards.Metamorphosis>()));
            }
        }
        finally
        {
            Squ.Cards.Metamorphosis.CandidateProvider = original;
        }

        AssertAllSelectionsConsumed(selector);
        Assert.True(selected.IsUpgraded);
        AssertInPile(selected, PileType.Hand);
        var power = Assert.IsType<MetamorphosisPower>(Player.Creature.GetPower<MetamorphosisPower>());
        Assert.True(power.HasRecorded(silentPool));
        Assert.Equal(1, power.Amount);
    }

    [Fact]
    public async Task No_candidates_finishes_without_selection_or_power()
    {
        IMetamorphosisCandidateProvider original = Squ.Cards.Metamorphosis.CandidateProvider;
        Squ.Cards.Metamorphosis.CandidateProvider = new FixedProvider([]);
        int handCount = PileType.Hand.GetPile(Player).Cards.Count;

        try
        {
            await Play(await AddToHand<Squ.Cards.Metamorphosis>());
        }
        finally
        {
            Squ.Cards.Metamorphosis.CandidateProvider = original;
        }

        Assert.Equal(handCount, PileType.Hand.GetPile(Player).Cards.Count);
        Assert.Null(Player.Creature.GetPower<MetamorphosisPower>());
    }

    [Fact]
    public async Task Selecting_the_same_character_pool_twice_does_not_increase_record_count()
    {
        var silentPool = ModelDb.CardPool<SilentCardPool>();
        IMetamorphosisCandidateProvider original = Squ.Cards.Metamorphosis.CandidateProvider;
        try
        {
            for (int play = 0; play < 2; play++)
            {
                CardModel selected = Combat.CreateCard<Neutralize>(Player);
                Squ.Cards.Metamorphosis.CandidateProvider = new FixedProvider(
                    [new MetamorphosisCandidate(selected, silentPool)]);
                var selector = new ScriptedCardSelector().ThenSelect(selected);
                using (UseCardSelector(selector))
                {
                    await Play(await AddToHand<Squ.Cards.Metamorphosis>());
                }
                AssertAllSelectionsConsumed(selector);
            }
        }
        finally
        {
            Squ.Cards.Metamorphosis.CandidateProvider = original;
        }

        var power = Assert.IsType<MetamorphosisPower>(Player.Creature.GetPower<MetamorphosisPower>());
        Assert.Equal(1, power.Amount);
        Assert.True(power.HasRecorded(silentPool));
    }
}
