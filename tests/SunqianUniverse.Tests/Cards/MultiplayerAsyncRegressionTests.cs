using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;
using Squ;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MultiplayerAsyncRegressionTests : SunqianCombatTestSuite
{
    private sealed class PerOwnerCardSelector(IReadOnlyDictionary<ulong, CardModel> selections)
        : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(
            IEnumerable<CardModel> options,
            int minSelect,
            int maxSelect)
        {
            CardModel[] available = options.ToArray();
            CardModel? selected = null;
            foreach (CardModel option in available)
            {
                if (selections.TryGetValue(option.Owner.NetId, out CardModel? candidate)
                    && Array.IndexOf(available, candidate) >= 0)
                {
                    selected = candidate;
                    break;
                }
            }
            return Task.FromResult<IEnumerable<CardModel>>(
                selected is null ? [] : [selected]);
        }

        public CardRewardSelection GetSelectedCardReward(
            IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) =>
            throw new InvalidOperationException("Combat reward selection was not expected.");
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddRemotePlayer<SunqianCharacter>()
            .AddEnemy<BigDummy>().WithSeed("multiplayer-async-regressions");

    [Fact]
    public async Task Concurrent_last_energy_attacks_keep_all_in_bonus_isolated_per_player()
    {
        var ally = Players.Single(player => player != Player);
        await ApplyPower<AllInPower>(
            Player.Creature,
            (int)AllInPower.UpgradedBonusPercent);
        await ApplyPower<AllInPower>(
            ally.Creature,
            (int)AllInPower.UpgradedBonusPercent);
        await PlayerCmd.SetEnergy(2, Player);
        await PlayerCmd.SetEnergy(1, ally);
        var ownerAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var allyAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;

        await Task.WhenAll(new Task[]
        {
            Play(ownerAttack, enemy),
            Play(allyAttack, enemy),
        });

        Assert.Equal(hp - 6 - 9, enemy.CurrentHp);
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        Assert.Equal(0, ally.PlayerCombatState!.Energy);
    }

    [Fact]
    public async Task Concurrent_family_exhausts_trigger_only_the_matching_players_power()
    {
        var ally = Players.Single(player => player != Player);
        await ApplyPower<NeverHadThesePower>(Player.Creature, 1);
        await ApplyPower<NeverHadThesePower>(ally.Creature, 1);
        await PlayerCmd.SetEnergy(0, Player);
        await PlayerCmd.SetEnergy(0, ally);
        var ownerFamily = await AddCardToPile<BelovedPartner>(PileType.Hand);
        var allyFamily = await AddCardToPile<BelovedPartner>(PileType.Hand, owner: ally);

        await Task.WhenAll(new Task[]
        {
            CardCmd.Exhaust(new BlockingPlayerChoiceContext(), ownerFamily),
            CardCmd.Exhaust(new BlockingPlayerChoiceContext(), allyFamily),
        });

        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        Assert.Equal(1, ally.PlayerCombatState!.Energy);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, 1);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(ally.Creature, 1);
    }

    [Fact]
    public async Task Concurrent_transform_choices_keep_cards_and_results_with_their_owners()
    {
        var ally = Players.Single(player => player != Player);
        await ApplyPower<DistributedScreenwriterPower>(Player.Creature, 1);
        await ApplyPower<DistributedScreenwriterPower>(ally.Creature, 1);
        var ownerSelected = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var allySelected = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        int ownerScripts = PileType.Hand.GetPile(Player).Cards.Count(card =>
            card.Tags.Contains(SquCardTags.Script));
        int allyScripts = PileType.Hand.GetPile(ally).Cards.Count(card =>
            card.Tags.Contains(SquCardTags.Script));
        var selector = new PerOwnerCardSelector(new Dictionary<ulong, CardModel>
        {
            [Player.NetId] = ownerSelected,
            [ally.NetId] = allySelected,
        });

        using (CardSelectCmd.UseSelector(selector))
        {
            await Task.WhenAll(new Task[]
            {
                Player.Creature.GetPower<DistributedScreenwriterPower>()!
                    .AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player),
                ally.Creature.GetPower<DistributedScreenwriterPower>()!
                    .AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ally),
            });
        }

        Assert.DoesNotContain(PileType.Hand.GetPile(Player).Cards, card =>
            ReferenceEquals(card, ownerSelected));
        Assert.DoesNotContain(PileType.Hand.GetPile(ally).Cards, card =>
            ReferenceEquals(card, allySelected));
        Assert.Equal(
            ownerScripts + 1,
            PileType.Hand.GetPile(Player).Cards.Count(card =>
                card.Tags.Contains(SquCardTags.Script) && card.Owner == Player));
        Assert.Equal(
            allyScripts + 1,
            PileType.Hand.GetPile(ally).Cards.Count(card =>
                card.Tags.Contains(SquCardTags.Script) && card.Owner == ally));
    }
}
