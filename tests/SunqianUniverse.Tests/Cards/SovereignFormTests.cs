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
using MegaCrit.Sts2.Core.Rooms;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Form_hover_previews_a_detached_native_blade_with_strike_name_and_tag(bool upgraded)
    {
        var form = await AddToHand<SovereignForm>();
        if (upgraded) Upgrade(form);
        var canonical = ModelDb.Card<SovereignBlade>();
        var pileCards = Player.PlayerCombatState!.AllCards.ToArray();
        foreach (var source in new CardModel[] { form, ModelDb.Card<SovereignForm>() })
        {
            var preview = Assert.Single(source.HoverTips.OfType<CardHoverTip>()).Card;
            Assert.IsType<SovereignBlade>(preview);
            Assert.Equal("君王打击", preview.Title);
            Assert.Contains(CardTag.Strike, preview.Tags);
            Assert.Null(preview.Owner);
            Assert.Null(preview.Pile);
            Assert.Equal(canonical.Id, preview.Id);
            Assert.Equal(canonical.Type, preview.Type);
            Assert.Equal(canonical.Rarity, preview.Rarity);
            Assert.Equal(canonical.TargetType, preview.TargetType);
            Assert.Equal(canonical.PortraitPath, preview.PortraitPath);
            Assert.Equal(canonical.EnergyCost.Canonical, preview.EnergyCost.Canonical);
            Assert.Equal(canonical.Keywords, preview.Keywords);
            Assert.Equal(canonical.GetDescriptionForPile(PileType.Deck), preview.GetDescriptionForPile(PileType.Deck));
        }
        Assert.Equal(pileCards, Player.PlayerCombatState.AllCards.ToArray());
        Assert.DoesNotContain(CardTag.Strike, canonical.Tags);
        Assert.DoesNotContain("君王打击", canonical.Title);
        var normal = await AddToHand<SovereignBlade>();
        Assert.DoesNotContain(CardTag.Strike, normal.Tags);
        Assert.DoesNotContain("君王打击", normal.Title);
    }

    [Theory]
    [InlineData(PileType.Hand)]
    [InlineData(PileType.Draw)]
    [InlineData(PileType.Discard)]
    [InlineData(PileType.Exhaust)]
    public async Task Form_grants_actual_strike_tag_in_every_combat_pile_and_removal_restores_it(PileType pile)
    {
        var blade = await AddToHand<SovereignBlade>();
        await CardPileCmd.Add(blade, pile);
        await ApplyPower<SovereignFormPower>(Player.Creature, 1);
        Assert.Equal("君王打击", blade.Title);
        Assert.Single(blade.Tags.Where(tag => tag == CardTag.Strike));
        await PowerCmd.Remove<SovereignFormPower>(Player.Creature);
        Assert.DoesNotContain(CardTag.Strike, blade.Tags);
        Assert.DoesNotContain("君王打击", blade.Title);
    }

    [Fact]
    public async Task Tracks_actual_spent_stacks_and_gains_do_not_reduce_the_total()
    {
        await Gain(20);
        await Gain(-2);
        await Gain(-4);
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
        await Gain(10);
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
        await Gain(-24);
        Assert.Equal(30, RighteousnessRunData.GetSpentAmount(Player));
        Assert.Equal(0, RighteousnessRunData.Get(Player));
        Assert.Equal(0, RighteousnessRunData.GetSpentAmount(Players.Single(player => player != Player)));
    }

    [Fact]
    public async Task Spent_total_survives_combat_cleanup_and_run_save_roundtrip()
    {
        await Gain(20);
        await Gain(-6);
        await Hook.AfterCombatEnd(Player.RunState, Combat, new CombatRoom((CombatState)Combat));
        Player.Creature.Reset();
        await Hook.BeforeCombatStart(Player.RunState, Combat);
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
        Assert.Equal(10, RighteousnessRunData.Get(Player));
        Type registry = AccessTools.TypeByName("STS2RitsuLib.RunData.RunSavedDataRegistry");
        string payload = Assert.IsType<string>(AccessTools.Method(registry, "BuildPayload").Invoke(null, [Player.RunState]));
        await Gain(-2);
        Assert.Equal(8, RighteousnessRunData.GetSpentAmount(Player));
        AccessTools.Method(registry, "ImportPayloadIntoRun").Invoke(null, [Player.RunState, payload]);
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
    }

    [Fact]
    public async Task Removing_positive_righteousness_counts_the_remaining_amount_once()
    {
        await Gain(12);
        await Gain(-2);
        await PowerCmd.Remove<RighteousnessPower>(Player.Creature);
        Assert.Equal(12, RighteousnessRunData.GetSpentAmount(Player));
        Assert.Equal(0, RighteousnessRunData.Get(Player));
    }

    [Fact]
    public async Task Combat_discard_does_not_increase_the_next_combats_sovereign_forge_total()
    {
        await Gain(27);
        await Gain(-6);
        await Hook.AfterCombatEnd(Player.RunState, Combat, new CombatRoom((CombatState)Combat));
        Assert.Equal(10, RighteousnessRunData.Get(Player));
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
        Player.Creature.Reset();
        await Hook.BeforeCombatStart(Player.RunState, Combat);
        var form = await AddToHand<SovereignForm>();
        Assert.Contains("此锻造值等于本局游戏你消耗的[gold]仁义[/gold]", form.GetDescriptionForPile(PileType.Hand));
        await Play(form);
        var blade = Assert.Single(Player.PlayerCombatState!.AllCards.OfType<SovereignBlade>());
        Assert.Equal(16, blade.DynamicVars.Damage.BaseValue);
        Assert.Equal(6, RighteousnessRunData.GetSpentAmount(Player));
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
        Assert.Equal(2, RighteousnessRunData.GetSpentAmount(Player));
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
