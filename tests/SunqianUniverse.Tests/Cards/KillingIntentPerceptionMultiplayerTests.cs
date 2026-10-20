using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KillingIntentPerceptionMultiplayerTests : SunqianCombatTestSuite
{
    private static readonly MethodInfo ChooseAttack = typeof(KillingIntentPerception).GetMethod(
        "ChooseAttack",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("killing-intent-perception-multiplayer");

    [Fact]
    public async Task Upgrade_lets_the_ally_select_a_playable_attack_from_their_hand()
    {
        var ally = Players.Single(player => player != Player);
        var selectedAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var otherAttack = await AddCardToPile<Bash>(PileType.Hand, owner: ally);
        var nonAttack = await AddCardToPile<DefendIronclad>(PileType.Hand, owner: ally);
        var card = Upgrade(await AddToHand<KillingIntentPerception>());
        var selector = new ScriptedCardSelector().ThenSelect(selectedAttack);

        CardModel? selected;
        using (UseCardSelector(selector))
        {
            var selectionTask = (Task<CardModel?>)ChooseAttack.Invoke(
                card,
                [new BlockingPlayerChoiceContext(), ally])!;
            selected = await selectionTask;
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Same(selectedAttack, selected);
        AssertInPile(selectedAttack, PileType.Hand);
        AssertInPile(otherAttack, PileType.Hand);
        AssertInPile(nonAttack, PileType.Hand);
    }

    [Fact]
    public async Task Random_multi_target_attack_played_at_an_enemy_always_includes_it()
    {
        var requiredTarget = EnemyAt(3);
        int hp = requiredTarget.CurrentHp;
        var salvo = await AddToHand<SalvoStrike>();

        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), salvo, requiredTarget);

        Assert.Equal(hp - SalvoStrike.BaseDamage, requiredTarget.CurrentHp);
        Assert.Equal(
            SalvoStrike.RandomEnemyTargetCount,
            Enumerable.Range(0, 4).Count(index => EnemyAt(index).CurrentHp < EnemyAt(index).MaxHp));
    }
}
