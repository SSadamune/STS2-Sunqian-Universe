using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SlightRevisionDerivedCardTests : SunqianCombatTestSuite
{
    private static readonly Type[] DerivedCardTypes =
    [
        typeof(BurnCity),
        typeof(FarmingGeneral),
        typeof(FireMeteorShower),
        typeof(LensPreShoot),
        typeof(SaidNotAfraidOfAcid),
    ];

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddEnemy<BigDummy>()
            .WithSeed("slight-revision-derived-card-registration");

    [Fact]
    public void Derived_cards_use_character_visuals_but_remain_library_visible_tokens()
    {
        HashSet<ModelId> rewardPoolIds = Player.Character.CardPool
            .GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)
            .Select(card => card.Id)
            .ToHashSet();
        CardPoolModel tokenPool = ModelDb.CardPool<TokenCardPool>();
        CardPoolModel characterPool = ModelDb.CardPool<SunqianCardPool>();

        foreach (Type cardType in DerivedCardTypes)
        {
            CardModel card = ModelDb.GetById<CardModel>(ModelDb.GetId(cardType));

            Assert.True(card.ShouldShowInCardLibrary);
            Assert.Equal(CardRarity.Token, card.Rarity);
            Assert.Same(tokenPool, card.Pool);
            Assert.Same(characterPool, card.VisualCardPool);
            Assert.DoesNotContain(card.Id, rewardPoolIds);
        }
    }
}
