using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using Squ.RunData;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class SovereignFormTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddRemotePlayer<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("sovereign-form");
    private static BlockingPlayerChoiceContext Choice() => new();
    private Task<RighteousnessPower?> Gain(int amount) => PowerCmd.Apply<RighteousnessPower>(Choice(), Player.Creature, amount, Player.Creature, null);

    [Fact]
    public async Task Tracks_actual_lost_stacks_not_loss_events_and_gains_do_not_reduce_the_total()
    {
        await Gain(20);
        await Gain(-2);
        await Gain(-4);
        Assert.Equal(6, RighteousnessRunData.GetLostAmount(Player));
        await Gain(10);
        Assert.Equal(6, RighteousnessRunData.GetLostAmount(Player));
        await Gain(-24);
        Assert.Equal(30, RighteousnessRunData.GetLostAmount(Player));
        Assert.Equal(0, RighteousnessRunData.Get(Player));
        Assert.Equal(0, RighteousnessRunData.GetLostAmount(Players.Single(player => player != Player)));
    }

    [Fact]
    public async Task Loss_total_survives_combat_cleanup_and_run_save_roundtrip()
    {
        await Gain(20);
        await Gain(-6);
        Player.Creature.Reset();
        await Hook.BeforeCombatStart(Player.RunState, Combat);
        Assert.Equal(6, RighteousnessRunData.GetLostAmount(Player));
        Assert.Equal(14, RighteousnessRunData.Get(Player));
        Type registry = AccessTools.TypeByName("STS2RitsuLib.RunData.RunSavedDataRegistry");
        string payload = Assert.IsType<string>(AccessTools.Method(registry, "BuildPayload").Invoke(null, [Player.RunState]));
        await Gain(-2);
        Assert.Equal(8, RighteousnessRunData.GetLostAmount(Player));
        AccessTools.Method(registry, "ImportPayloadIntoRun").Invoke(null, [Player.RunState, payload]);
        Assert.Equal(6, RighteousnessRunData.GetLostAmount(Player));
    }

    [Fact]
    public async Task Removing_positive_righteousness_counts_the_remaining_amount_once()
    {
        await Gain(12);
        await Gain(-2);
        await PowerCmd.Remove<RighteousnessPower>(Player.Creature);
        Assert.Equal(12, RighteousnessRunData.GetLostAmount(Player));
        Assert.Equal(0, RighteousnessRunData.Get(Player));
    }

    [Fact]
    public async Task Form_forges_the_run_total_and_renames_existing_and_future_blades_as_strikes()
    {
        await Gain(20);
        await Gain(-6);
        var old = await AddToHand<SovereignBlade>();
        Assert.DoesNotContain(CardTag.Strike, old.Tags);
        var card = await AddToHand<SovereignForm>();
        Assert.Contains("锻造[/gold]6", card.GetDescriptionForPile(PileType.Hand));
        await Play(card);
        Assert.Equal(16, old.DynamicVars.Damage.BaseValue);
        Assert.Equal("君王打击", old.Title);
        Assert.Contains(CardTag.Strike, old.Tags);
        var later = await AddToHand<SovereignBlade>();
        Assert.Equal("君王打击", later.Title);
        Assert.Contains(CardTag.Strike, later.Tags);
        var other = Players.Single(player => player != Player);
        var foreign = Combat.CreateCard<SovereignBlade>(other);
        Assert.DoesNotContain(CardTag.Strike, foreign.Tags);
        Assert.DoesNotContain("君王打击", foreign.Title);
        Assert.DoesNotContain(CardTag.Strike, ModelDb.Card<SovereignBlade>().Tags);
        await PowerCmd.Remove<SovereignFormPower>(Player.Creature);
        Assert.DoesNotContain(CardTag.Strike, old.Tags);
        Assert.DoesNotContain("君王打击", old.Title);
    }

    [Fact]
    public async Task Renamed_blade_participates_in_righteousness_replays_and_spending()
    {
        await Play(await AddToHand<SovereignForm>());
        await PlayerCmd.SetEnergy(3, Player);
        await Gain(10);
        var blade = Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<SovereignBlade>());
        int hp = EnemyAt(0).CurrentHp;
        await Play(blade, EnemyAt(0));
        Assert.Equal(hp - 20, EnemyAt(0).CurrentHp);
        Assert.Equal(8, RighteousnessRunData.Get(Player));
        Assert.Equal(2, RighteousnessRunData.GetLostAmount(Player));
    }

    [Fact]
    public async Task Upgrade_adds_retain_keeps_three_energy_and_unowned_previews_show_zero()
    {
        await Gain(20);
        await Gain(-6);
        var card = await AddToHand<SovereignForm>();
        Assert.Equal(3, card.EnergyCost.GetResolved());
        Assert.DoesNotContain(CardKeyword.Retain, card.Keywords);
        Upgrade(card);
        Assert.Equal(3, card.EnergyCost.GetResolved());
        Assert.Contains(CardKeyword.Retain, card.Keywords);
        Assert.Contains("锻造[/gold]0", ModelDb.Card<SovereignForm>().GetDescriptionForPile(PileType.Deck));
        var unowned = ModelDb.Card<SovereignForm>().ToMutable();
        Assert.Contains("锻造[/gold]0", unowned.GetDescriptionForPile(PileType.Deck));
        Assert.Contains("锻造[/gold]6", card.GetDescriptionForPile(PileType.Hand));
    }
}
