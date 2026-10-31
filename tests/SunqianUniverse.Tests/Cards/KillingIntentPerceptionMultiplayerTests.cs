using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KillingIntentPerceptionMultiplayerTests : SunqianCombatTestSuite
{
    private static readonly MethodInfo ChooseAttacks = typeof(KillingIntentPerception).GetMethod(
        "ChooseAttacks",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ChooseAlly = typeof(KillingIntentPerception).GetMethod(
        "ChooseAlly",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static Player? _forcedAlly;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("killing-intent-perception-multiplayer");

    [Fact]
    public async Task Upgrade_lets_the_ally_select_up_to_two_playable_attacks_from_their_hand()
    {
        var ally = Players.Single(player => player != Player);
        var firstAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var secondAttack = await AddCardToPile<Bash>(PileType.Hand, owner: ally);
        var nonAttack = await AddCardToPile<DefendIronclad>(PileType.Hand, owner: ally);
        var card = Upgrade(await AddToHand<KillingIntentPerception>());
        var selector = new ScriptedCardSelector().ThenSelect(firstAttack, secondAttack);

        List<CardModel> selected;
        using (UseCardSelector(selector))
        {
            var selectionTask = (Task<List<CardModel>>)ChooseAttacks.Invoke(
                card,
                [new BlockingPlayerChoiceContext(), ally])!;
            selected = await selectionTask;
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal([firstAttack, secondAttack], selected);
        AssertInPile(firstAttack, PileType.Hand);
        AssertInPile(secondAttack, PileType.Hand);
        AssertInPile(nonAttack, PileType.Hand);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ally_may_skip_without_selecting_an_attack(bool upgraded)
    {
        var ally = Players.Single(player => player != Player);
        await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var card = await AddToHand<KillingIntentPerception>();
        if (upgraded)
        {
            Upgrade(card);
        }

        var selector = new ScriptedCardSelector().ThenSkip();
        List<CardModel> selected;
        using (UseCardSelector(selector))
        {
            var selectionTask = (Task<List<CardModel>>)ChooseAttacks.Invoke(
                card,
                [new BlockingPlayerChoiceContext(), ally])!;
            selected = await selectionTask;
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Empty(selected);
    }

    [Fact]
    public async Task Both_selected_attacks_inherit_supreme_general_vigor()
    {
        const int vigor = 4;
        var ally = Players.Single(player => player != Player);
        var firstAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var secondAttack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        var selector = new ScriptedCardSelector().ThenSelect(firstAttack, secondAttack);
        var harmony = new Harmony("squ.tests.killing-intent-force-ally");
        _forcedAlly = ally;
        harmony.Patch(
            ChooseAlly,
            prefix: new HarmonyMethod(
                typeof(KillingIntentPerceptionMultiplayerTests),
                nameof(ForceAlly)));
        int hp = EnemyAt(0).CurrentHp;
        decimal strikeDamage = ModelDb.Card<StrikeIronclad>().DynamicVars.Damage.BaseValue;

        try
        {
            using (UseCardSelector(selector))
            {
                await Play(Upgrade(await AddToHand<KillingIntentPerception>()));
            }
        }
        finally
        {
            _forcedAlly = null;
            harmony.UnpatchAll(harmony.Id);
        }

        AssertAllSelectionsConsumed(selector);
        int expectedDamage = KillingIntentPerception.UpgradedDamage + vigor
            + (int)((strikeDamage + vigor) * 2m);
        Assert.Equal(hp - expectedDamage, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        AssertInPile(firstAttack, PileType.Discard);
        AssertInPile(secondAttack, PileType.Discard);
    }

    [Fact]
    public async Task Auto_played_x_cost_attack_captures_current_energy_without_spending_it()
    {
        const int remainingEnergy = 3;
        var ally = Players.Single(player => player != Player);
        await PlayerCmd.SetEnergy(remainingEnergy, ally);
        var xAttack = await AddCardToPile<FireMeteorShower>(PileType.Hand, owner: ally);

        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), xAttack, null);

        Assert.Equal(remainingEnergy, ally.PlayerCombatState!.Energy);
        Assert.Equal(remainingEnergy, xAttack.EnergyCost.GetResolved());
    }

    private static bool ForceAlly(ref Task<Player?> __result)
    {
        __result = Task.FromResult(_forcedAlly);
        return false;
    }
}
