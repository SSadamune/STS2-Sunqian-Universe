using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TheBackupsBackupTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<SunqianCharacter>().AddRemotePlayer<SunqianCharacter>().AddEnemy<BigDummy>().WithSeed("the-backups-backup");

    [Fact]
    public async Task Options_exclude_the_backups_backup_in_both_directions_when_decks_have_at_most_three_eligible_cards()
    {
        var ally = Players.Single(player => player != Player);
        var ownerCards = new CardModel[]
        {
            Combat.CreateCard<TheBackupsBackup>(Player),
            Combat.CreateCard<FlameStrike>(Player),
            Combat.CreateCard<Phasing>(Player),
        };
        var allyCards = new CardModel[]
        {
            Combat.CreateCard<TheBackupsBackup>(ally),
            Combat.CreateCard<TableFlip>(ally),
            Combat.CreateCard<KeepCalm>(ally),
        };
        Player.Deck.Clear();
        ally.Deck.Clear();
        foreach (CardModel card in ownerCards)
        {
            Player.Deck.AddInternal(card);
        }
        foreach (CardModel card in allyCards)
        {
            ally.Deck.AddInternal(card);
        }

        CardModel[]? ownerOptions = null;
        CardModel[]? allyOptions = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
            {
                ownerOptions = options.ToArray();
                return [options[0]];
            })
            .ThenSelect(options =>
            {
                allyOptions = options.ToArray();
                return [options[0]];
            });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<TheBackupsBackup>(), ally.Creature);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(3, Player.Deck.Cards.Count(card => card.Rarity is CardRarity.Uncommon or CardRarity.Rare));
        Assert.Equal(3, ally.Deck.Cards.Count(card => card.Rarity is CardRarity.Uncommon or CardRarity.Rare));

        Assert.NotNull(ownerOptions);
        Assert.Equal(3, ownerOptions.Length);
        Assert.DoesNotContain(ownerOptions, card => card.CanonicalInstance is TheBackupsBackup);
        Assert.Contains(ownerOptions, card => ReferenceEquals(card, allyCards[1]));
        Assert.Contains(ownerOptions, card => ReferenceEquals(card, allyCards[2]));

        Assert.NotNull(allyOptions);
        Assert.Equal(3, allyOptions.Length);
        Assert.DoesNotContain(allyOptions, card => card.CanonicalInstance is TheBackupsBackup);
        Assert.Contains(allyOptions, card => ReferenceEquals(card, ownerCards[1]));
        Assert.Contains(allyOptions, card => ReferenceEquals(card, ownerCards[2]));
    }

    [Fact]
    public async Task Upgrade_fills_with_three_non_common_non_ancient_cards_when_decks_are_common_only()
    {
        var ally = Players.Single(player => player != Player);
        var offeredSets = new List<CardModel[]>();
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
            {
                offeredSets.Add(options.ToArray());
                return [options[0]];
            })
            .ThenSelect(options =>
            {
                offeredSets.Add(options.ToArray());
                return [options[0]];
            });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<TheBackupsBackup>()), ally.Creature);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, offeredSets.Count);
        Assert.All(offeredSets, offered =>
        {
            Assert.Equal(3, offered.Length);
            Assert.All(offered, card =>
            {
                Assert.NotEqual(CardRarity.Common, card.Rarity);
                Assert.NotEqual(CardRarity.Ancient, card.Rarity);
            });
        });
    }

    [Fact]
    public async Task Each_player_receives_the_copy_they_selected_from_the_other_deck()
    {
        var ally = Players.Single(player => player != Player);
        CardModel? ownerSelection = null;
        CardModel? allySelection = null;
        var selector = new ScriptedCardSelector()
            .ThenSelect(options =>
            {
                ownerSelection = options[0];
                return [ownerSelection];
            })
            .ThenSelect(options =>
            {
                allySelection = options[0];
                return [allySelection];
            });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<TheBackupsBackup>(), ally.Creature);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(ownerSelection);
        Assert.NotNull(allySelection);
        Assert.Contains(
            PileType.Hand.GetPile(Player).Cards,
            card => card.Id == ownerSelection.Id && card.Owner == Player);
        Assert.Contains(
            PileType.Hand.GetPile(ally).Cards,
            card => card.Id == allySelection.Id && card.Owner == ally);
    }

    [Fact]
    public async Task Card_is_multiplayer_only_zero_cost_and_upgradeable()
    {
        var card = Upgrade(await AddToHand<TheBackupsBackup>());
        Assert.True(card.IsUpgraded);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Equal(CardMultiplayerConstraint.MultiplayerOnly, card.MultiplayerConstraint);
    }
}
