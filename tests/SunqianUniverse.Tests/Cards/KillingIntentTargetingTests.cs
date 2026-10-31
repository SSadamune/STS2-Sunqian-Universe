using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KillingIntentTargetingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>(2).AddRemotePlayer<Ironclad>(3)
            .AddEnemy<BigDummy>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("killing-intent-required-target");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upgrade_lets_each_ally_play_one_attack_and_random_attacks_include_the_target(bool nativeRandom)
    {
        var allies = Players.Where(player => player != Player).ToArray();
        CardModel random = nativeRandom
            ? await AddCardToPile<SwordBoomerang>(PileType.Hand, owner: allies[0])
            : await AddCardToPile<SalvoStrike>(PileType.Hand, owner: allies[0]);
        var strike = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: allies[1]);
        var target = EnemyAt(3);
        int hp = target.CurrentHp;
        var selector = new ScriptedCardSelector().ThenSelect(random).ThenSelect(strike);
        using (UseCardSelector(selector))
            await Play(Upgrade(await AddToHand<KillingIntentPerception>()), target);
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, selector.SelectionRequestCount);
        int minimumDamage = 13 + (nativeRandom ? 3 : SalvoStrike.BaseDamage) + 6;
        Assert.True(target.CurrentHp <= hp - minimumDamage);
        AssertInPile(strike, PileType.Discard);
        AssertInPile(random, nativeRandom ? PileType.Discard : PileType.Exhaust);
        Assert.All(allies, ally => Assert.Empty(PileType.Play.GetPile(ally).Cards));
    }

    [Fact]
    public async Task Killing_the_target_does_not_redirect_allies_to_a_different_enemy()
    {
        var ally = Players.First(player => player != Player);
        var strike = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        await CreatureCmd.SetCurrentHp(EnemyAt(3), 1);
        int otherHp = EnemyAt(0).CurrentHp;
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector))
            await Play(Upgrade(await AddToHand<KillingIntentPerception>()), EnemyAt(3));
        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.Equal(otherHp, EnemyAt(0).CurrentHp);
        AssertInPile(strike, PileType.Hand);
    }
}
