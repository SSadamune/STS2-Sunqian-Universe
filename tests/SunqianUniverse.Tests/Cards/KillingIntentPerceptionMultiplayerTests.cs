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
    private static int _nestedChoices;

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

    [Fact]
    public async Task After_what_to_eat_nested_killing_intent_choices_finish_without_reusing_a_paused_action()
    {
        var ally = Players.Single(player => player != Player);
        foreach (var player in Players)
            foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray())
                await CardPileCmd.Add(card, PileType.Discard);
        var food = await AddToHand<WhatToEat>();
        var firstExhaust = await AddToHand<Wound>();
        var secondExhaust = await AddToHand<Wound>();
        var first = await AddToHand<KillingIntentPerception>();
        var nested = await AddCardToPile<KillingIntentPerception>(PileType.Hand, owner: ally);
        var attack = await AddToHand<StrikeIronclad>();
        await PlayerCmd.SetEnergy(10, Player);
        var selector = new ScriptedCardSelector()
            .ThenSelect(firstExhaust, secondExhaust)
            .ThenSelect(nested)
            .ThenSelect(attack);
        var harmony = new Harmony("squ.tests.killing-intent-nested-context");
        _nestedChoices = 0;
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand)),
            prefix: new HarmonyMethod(typeof(KillingIntentPerceptionMultiplayerTests), nameof(CheckNestedChoiceContext)));
        int hp = EnemyAt(0).CurrentHp;
        try
        {
            using (UseCardSelector(selector))
            {
                await Play(food);
                await Play(first);
            }
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, _nestedChoices);
        Assert.Equal(hp - KillingIntentPerception.BaseDamage * 2 - 6, EnemyAt(0).CurrentHp);
        AssertInPile(food, PileType.Discard);
        AssertInPile(first, PileType.Discard);
        AssertInPile(nested, PileType.Discard);
        AssertInPile(attack, PileType.Discard);
        Assert.Empty(PileType.Play.GetPile(Player).Cards);
        Assert.Empty(PileType.Play.GetPile(ally).Cards);
    }

    private static bool ChooseOtherPlayer(KillingIntentPerception __instance, ref Task<Player?> __result)
    {
        __result = Task.FromResult<Player?>(__instance.Owner.RunState.Players.Single(
            player => player != __instance.Owner));
        return false;
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(5, 3)]
    public async Task Cross_player_attack_combines_inherited_and_ally_vigor(int ownerVigor, int allyVigor)
    {
        var ally = Players.Single(player => player != Player);
        var attack = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        await ApplyPower<VigorPower>(Player.Creature, ownerVigor);
        if (allyVigor > 0) await ApplyPower<VigorPower>(ally.Creature, allyVigor);
        int hp = EnemyAt(0).CurrentHp;
        var selector = new ScriptedCardSelector().ThenSelect(attack);
        var harmony = new Harmony("squ.tests.killing-intent-cross-player-vigor");
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        try
        {
            using (UseCardSelector(selector))
                await Play(await AddToHand<KillingIntentPerception>());
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(hp - (9 + ownerVigor) - (6 + ownerVigor + allyVigor), EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        Assert.Null(ally.Creature.GetPower<VigorPower>());
    }

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(5, 3, true)]
    [InlineData(0, 3, false)]
    [InlineData(5, 0, false)]
    public async Task Nested_supreme_generals_pass_both_players_vigor_to_the_returning_attack(
        int ownerVigor, int allyVigor, bool upgraded)
    {
        var ally = Players.Single(player => player != Player);
        var first = await AddToHand<KillingIntentPerception>();
        var nested = await AddCardToPile<KillingIntentPerception>(PileType.Hand, owner: ally);
        var attack = await AddToHand<StrikeIronclad>();
        if (upgraded) { Upgrade(first); Upgrade(nested); }
        if (ownerVigor > 0) await ApplyPower<VigorPower>(Player.Creature, ownerVigor);
        if (allyVigor > 0) await ApplyPower<VigorPower>(ally.Creature, allyVigor);
        int hp = EnemyAt(0).CurrentHp;
        int afterFirst = hp;
        int afterNested = hp;
        var selector = new ScriptedCardSelector()
            .ThenSelect(_ => { afterFirst = EnemyAt(0).CurrentHp; return [nested]; })
            .ThenSelect(_ => { afterNested = EnemyAt(0).CurrentHp; return [attack]; });
        var harmony = new Harmony("squ.tests.killing-intent-nested-vigor");
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        try
        {
            using (UseCardSelector(selector))
                await Play(first);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        int baseDamage = upgraded ? 11 : 9;
        Assert.Equal(baseDamage + ownerVigor, hp - afterFirst);
        Assert.Equal(baseDamage + ownerVigor + allyVigor, afterFirst - afterNested);
        Assert.Equal(6 + ownerVigor + allyVigor, afterNested - EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        Assert.Null(ally.Creature.GetPower<VigorPower>());
        AssertInPile(first, PileType.Discard);
        AssertInPile(nested, PileType.Discard);
        AssertInPile(attack, PileType.Discard);
        int afterChain = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));
        Assert.Equal(6, afterChain - EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Nested_generals_own_vigor_does_not_leak_into_the_outer_generals_next_attack()
    {
        var ally = Players.Single(player => player != Player);
        var first = Upgrade(await AddToHand<KillingIntentPerception>());
        var nested = await AddCardToPile<KillingIntentPerception>(PileType.Hand, owner: ally);
        var returningAttack = await AddToHand<StrikeIronclad>();
        var sibling = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        await ApplyPower<VigorPower>(Player.Creature, 5);
        await ApplyPower<VigorPower>(ally.Creature, 3);
        int hp = EnemyAt(0).CurrentHp;
        var selector = new ScriptedCardSelector().ThenSelect(nested, sibling).ThenSelect(returningAttack);
        var harmony = new Harmony("squ.tests.killing-intent-scoped-nested-vigor");
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        try
        {
            using (UseCardSelector(selector)) await Play(first);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        // A+ = 11+5; B = 9+8; returning strike = 6+8; sibling = 6+5.
        Assert.Equal(16 + 17 + 14 + 11, hp - EnemyAt(0).CurrentHp);
    }

    private static void CheckNestedChoiceContext(PlayerChoiceContext context, AbstractModel source)
    {
        if (source is KillingIntentPerception)
        {
            // Both directions must remain in the current action, including when
            // the nested attack asks the original player to choose again.
            Assert.Null(context.OwnerId);
            _nestedChoices++;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Killing_intent_into_ally_blitzkrieg_keeps_eight_vigor_through_recursive_blitzkrieg_and_strikes(bool upgraded)
    {
        var ally = Players.Single(player => player != Player);
        await CardPileCmd.Add(PileType.Hand.GetPile(ally).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Draw.GetPile(ally).Cards.ToArray(), PileType.Exhaust);
        var blitzkrieg = await AddCardToPile<Blitzkrieg>(PileType.Hand, owner: ally);
        var nestedBlitzkrieg = await AddCardToPile<Blitzkrieg>(PileType.Hand, owner: ally);
        if (upgraded) { Upgrade(blitzkrieg); Upgrade(nestedBlitzkrieg); }
        var handStrike = await AddCardToPile<StrikeIronclad>(PileType.Hand, owner: ally);
        var drawStrike = await AddToDrawPileTop<StrikeIronclad>(ally);
        await ApplyPower<VigorPower>(Player.Creature, 5);
        await ApplyPower<VigorPower>(ally.Creature, 3);
        await PlayerCmd.SetEnergy(0, ally);
        int hp = EnemyAt(0).CurrentHp;
        int afterFirst = hp;
        var selector = new ScriptedCardSelector().ThenSelect(_ =>
        {
            afterFirst = EnemyAt(0).CurrentHp;
            return [blitzkrieg];
        });
        var harmony = new Harmony("squ.tests.killing-intent-into-blitzkrieg");
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        try
        {
            using (UseCardSelector(selector)) await Play(await AddToHand<KillingIntentPerception>());
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(14, hp - afterFirst);
        int blitzDamage = upgraded ? 15 : 11;
        Assert.Equal((blitzDamage + 8) * 2 + (6 + 8) * 2, afterFirst - EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        Assert.Null(ally.Creature.GetPower<VigorPower>());
        Assert.Equal(0, ally.PlayerCombatState!.Energy);
        foreach (var card in new CardModel[] { blitzkrieg, nestedBlitzkrieg, handStrike, drawStrike })
            AssertInPile(card, PileType.Discard);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Killing_intent_into_ally_part_time_general_passes_eight_vigor_to_every_generated_attack(bool upgraded)
    {
        var ally = Players.Single(player => player != Player);
        var general = await AddCardToPile<PartTimeGeneralScript>(PileType.Hand, owner: ally);
        if (upgraded) Upgrade(general);
        await ApplyPower<VigorPower>(Player.Creature, 5);
        await ApplyPower<VigorPower>(ally.Creature, 3);
        await PlayerCmd.SetEnergy(2, ally);
        var expectedStrike = Combat.CreateCard<PommelStrike>(ally);
        if (upgraded) Upgrade(expectedStrike);
        int strikeDamage = expectedStrike.DynamicVars.Damage.IntValue;
        expectedStrike.RemoveFromState();
        int hp = EnemyAt(0).CurrentHp;
        var selector = new ScriptedCardSelector().ThenSelect(general);
        var harmony = new Harmony("squ.tests.killing-intent-into-part-time-general");
        harmony.Patch(ChooseAlly, prefix: new HarmonyMethod(
            typeof(KillingIntentPerceptionMultiplayerTests), nameof(ChooseOtherPlayer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PartTimeGeneralScript), "ChooseStrikeCards"),
            prefix: new HarmonyMethod(typeof(KillingIntentPerceptionMultiplayerTests), nameof(ForceTwoPommelStrikes)));
        try
        {
            using (UseCardSelector(selector)) await Play(await AddToHand<KillingIntentPerception>());
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(14 + (strikeDamage + 8) * 2, hp - EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        Assert.Null(ally.Creature.GetPower<VigorPower>());
        Assert.Equal(2, ally.PlayerCombatState!.Energy);
        AssertInPile(general, PileType.Exhaust);
        Assert.NotNull(ally.Creature.GetPower<ScriptPartTimeGeneralPower>());
        var record = Assert.Single(Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(ally).Cards
            .OfType<PartTimeGeneralScriptRecord>());
        Assert.Equal(2, record.RecordedCards.Count);
    }

    private static bool ForceTwoPommelStrikes(int count, ref List<CardModel> __result)
    {
        Assert.Equal(2, count);
        __result = [ModelDb.Card<PommelStrike>(), ModelDb.Card<PommelStrike>()];
        return false;
    }

    private static bool ForceAlly(ref Task<Player?> __result)
    {
        __result = Task.FromResult(_forcedAlly);
        return false;
    }
}
