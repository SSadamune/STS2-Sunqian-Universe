using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using Squ.RunData;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RighteousnessTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddRemotePlayer<LiubeiCharacter>()
            .AddEnemy<BigDummy>().WithSeed("righteousness");

    [Theory]
    [InlineData(-12, 1, -12)]
    [InlineData(0, 1, 0)]
    [InlineData(9, 1, 9)]
    [InlineData(10, 2, 8)]
    [InlineData(19, 2, 17)]
    [InlineData(20, 3, 16)]
    [InlineData(29, 3, 25)]
    [InlineData(30, 4, 24)]
    public async Task Strike_replays_and_spends_once_per_original_play(int initial, int plays, int remaining)
    {
        await Gain(initial);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));
        Assert.Equal(hp - 6 * plays, EnemyAt(0).CurrentHp);
        Assert.Equal(remaining, RighteousnessRunData.Get(Player));
        Assert.Equal(remaining, Player.Creature.GetPower<RighteousnessPower>()?.Amount ?? 0);
    }

    [Theory]
    [InlineData(typeof(StrikeLiubei))]
    [InlineData(typeof(StrikeLongtao))]
    [InlineData(typeof(StrikeIronclad))]
    [InlineData(typeof(FlameStrike))]
    [InlineData(typeof(FingerSnapStrike))]
    [InlineData(typeof(RuthlessStrike))]
    [InlineData(typeof(FlyingSwordStrike))]
    [InlineData(typeof(StarryNightStrike))]
    public async Task Replays_are_part_of_one_manual_play_and_never_reenter_righteousness(Type cardType)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Gain(20);
        CardModel card = await AddToHand(ModelDb.GetId(cardType));
        int playsBefore = CombatManager.Instance.History.CardPlaysStarted
            .Count(entry => ReferenceEquals(entry.CardPlay.Card, card));

        await Play(card, EnemyAt(0));

        int playsAfter = CombatManager.Instance.History.CardPlaysStarted
            .Count(entry => ReferenceEquals(entry.CardPlay.Card, card));
        Assert.Equal(3, playsAfter - playsBefore);
        Assert.Equal(16, RighteousnessRunData.Get(Player));
        Assert.Equal(16, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
    }

    [Fact]
    public async Task Each_new_strike_recalculates_threshold_after_the_previous_spending()
    {
        await Gain(20);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));
        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));
        Assert.Equal(hp - 30, EnemyAt(0).CurrentHp);
        Assert.Equal(14, RighteousnessRunData.Get(Player));
    }

    [Fact]
    public async Task Non_strikes_do_not_spend_or_replay_and_dual_swords_adds_to_strike_replays()
    {
        await Gain(20);
        await Play(await AddToHand<DefendLiubei>());
        Assert.Equal(5, Player.Creature.Block);
        Assert.Equal(20, RighteousnessRunData.Get(Player));
        await ApplyPower<BenevolenceRighteousnessDualSwordsPower>(Player.Creature, 1);
        int hp = EnemyAt(0).CurrentHp;
        await Play(await AddToHand<StrikeLiubei>(), EnemyAt(0));
        Assert.Equal(hp - 24, EnemyAt(0).CurrentHp);
        Assert.Equal(16, RighteousnessRunData.Get(Player));
    }

    [Theory]
    [InlineData(false, 6, 8)]
    [InlineData(true, 8, 10)]
    public async Task Starter_skill_loses_hp_then_gains_resource_and_block(bool upgraded, int resource, int block)
    {
        int hp = Player.Creature.CurrentHp;
        var card = await AddToHand<StandByRighteousness>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardRarity.Basic, card.Rarity);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        await Play(card);
        Assert.Equal(hp - 4, Player.Creature.CurrentHp);
        Assert.Equal(resource, RighteousnessRunData.Get(Player));
        Assert.Equal(block, Player.Creature.Block);
        AssertInPile(card, PileType.Discard);
    }

    [Fact]
    public async Task Skill_hp_loss_uses_temporary_health_and_tungsten_rod()
    {
        int real = Player.Creature.CurrentHp;
        var rod = (TungstenRod)MegaCrit.Sts2.Core.Models.ModelDb.Relic<TungstenRod>().ToMutable();
        Player.AddRelicInternal(rod, silent: true);
        await ApplyPower<TemporaryHealthPower>(Player.Creature, 10);
        await Play(await AddToHand<StandByRighteousness>());
        Assert.Equal(real + 7, Player.Creature.CurrentHp);
        Assert.Equal(7, Player.Creature.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(6, RighteousnessRunData.Get(Player));
    }

    [Theory]
    [InlineData(25)]
    [InlineData(-13)]
    public async Task Resource_survives_combat_cleanup_and_restores_on_next_combat(int amount)
    {
        await Gain(amount);
        Player.Creature.Reset();
        Assert.Null(Player.Creature.GetPower<RighteousnessPower>());
        Assert.Equal(amount, RighteousnessRunData.Get(Player));
        await Hook.BeforeCombatStart(Player.RunState, Combat);
        Assert.Equal(amount, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
        // Repeated start/resume events must not double the amount.
        await Hook.BeforeCombatStart(Player.RunState, Combat);
        Assert.Equal(amount, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
    }

    [Fact]
    public async Task Per_player_saved_state_roundtrips_positive_and_negative_values()
    {
        var other = Players.Single(player => player != Player);
        await Gain(20);
        await PowerCmd.Apply<RighteousnessPower>(new BlockingPlayerChoiceContext(),
            other.Creature, -7, other.Creature, null);
        Type registry = AccessTools.TypeByName("STS2RitsuLib.RunData.RunSavedDataRegistry");
        string payload = Assert.IsType<string>(AccessTools.Method(registry, "BuildPayload")
            .Invoke(null, [Player.RunState]));
        Assert.Contains("righteousness", payload);
        RighteousnessRunData.Set(Player, 0);
        RighteousnessRunData.Set(other, 0);
        AccessTools.Method(registry, "ImportPayloadIntoRun").Invoke(null, [Player.RunState, payload]);
        Assert.Equal(20, RighteousnessRunData.Get(Player));
        Assert.Equal(-7, RighteousnessRunData.Get(other));
    }

    [Fact]
    public async Task Gaining_resource_can_pay_off_a_negative_balance()
    {
        await Gain(-8);
        await Play(await AddToHand<StandByRighteousness>());
        Assert.Equal(-2, Player.Creature.GetPower<RighteousnessPower>()!.Amount);
        Assert.Equal(-2, RighteousnessRunData.Get(Player));
        await Gain(2);
        Assert.Equal(0, RighteousnessRunData.Get(Player));
        Assert.Null(Player.Creature.GetPower<RighteousnessPower>());
    }

    [Fact]
    public async Task Hover_uses_current_dynamic_amount_and_updates_after_upgrade()
    {
        var card = await AddToHand<StandByRighteousness>();
        var before = Assert.Single(card.HoverTips.OfType<HoverTip>()
            .Where(tip => tip.Description.Contains("6") && tip.Description.Contains("10")));
        Assert.Contains("6", before.Description);
        Assert.EndsWith("6", before.Title);
        Assert.Contains(card.HoverTips,
            tip => tip.Id == HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards).Id);
        Upgrade(card);
        var after = Assert.Single(card.HoverTips.OfType<HoverTip>()
            .Where(tip => tip.Description.Contains("8") && tip.Description.Contains("10")));
        Assert.NotEqual(before.Description, after.Description);
        Assert.EndsWith("8", after.Title);
    }

    private Task<RighteousnessPower?> Gain(int amount) =>
        PowerCmd.Apply<RighteousnessPower>(new BlockingPlayerChoiceContext(),
            Player.Creature, amount, Player.Creature, null);
}
