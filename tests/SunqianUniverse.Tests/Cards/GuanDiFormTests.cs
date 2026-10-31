using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GuanDiFormTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("guan-di-scripts");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Vakuu_auto_play_selects_legal_scripts_without_waiting_for_manual_selection(bool existingRecords)
    {
        var form = await AddToHand<GuanDiForm>();
        if (existingRecords)
        {
            var context = new BlockingPlayerChoiceContext();
            await Squ.Script.ScriptSystem.UnlockOrUpgradeAsync<GuanYuCivilVer>(context, form, 2, activate: false);
            await Squ.Script.ScriptSystem.UnlockOrUpgradeAsync<GuanYuMartialVer>(context, form, 2, activate: false);
            var servant = await AddToHand<ServantScript>();
            await Squ.Script.ScriptSystem.UnlockOrUpgradeAsync<ServantScriptRecord>(context, servant, 1, activate: false);
        }
        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), form, null).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.Null(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Equal(existingRecords ? 2 : 1, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.Equal(existingRecords, Player.Creature.GetPower<ScriptServantPower>() is not null);
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
        Assert.Equal(existingRecords ? (1, 2) : (1, 1), Squ.Script.ScriptSystem.GetSelectionBounds(Player, records));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Play_adds_both_scripts_and_one_activation_slot(bool upgraded)
    {
        var sunqian = await AddToDrawPileTop<SunqianScript>();
        var card = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(card);
        Assert.Equal(3, card.EnergyCost.GetResolved());
        var selector = new ScriptedCardSelector().ThenSelectWhere(candidate => candidate is GuanYuCivilVer);
        using (UseCardSelector(selector)) { await Play(card); }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(1, selector.SelectionRequestCount);
        Assert.DoesNotContain(SquCardTags.Script, card.Tags);
        Assert.DoesNotContain(SquKeywords.Script, card.Keywords);
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        var civil = Assert.Single(records.OfType<GuanYuCivilVer>());
        var martial = Assert.Single(records.OfType<GuanYuMartialVer>());
        Assert.Equal(2, records.Count);
        Assert.True(civil.CountsTowardActivationLimit);
        Assert.True(martial.CountsTowardActivationLimit);
        Assert.False(civil.ShouldShowInCardLibrary);
        Assert.False(martial.ShouldShowInCardLibrary);
        Assert.Equal(upgraded ? 4 : 2, civil.StoredAmount);
        Assert.Equal(upgraded ? 4 : 2, martial.StoredAmount);
        Assert.Equal(2, Squ.Script.ScriptSystem.GetActivationLimit(Player));
        AssertOnlyScript<GuanYuCivilVerPower>();
        AssertInPile(sunqian, PileType.Draw);
        Assert.Equal(0, Player.Creature.Block);
    }

    [Fact]
    public async Task Form_and_full_regular_slots_coexist_and_switching_forms_only_removes_the_other_form()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<ServantScript>());
        await PlayForm(await AddToHand<GuanDiForm>());
        await Activate<GuanYuCivilVer>();
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        await Play(await AddToHand<SunqianScript>());
        Assert.Equal(3, Squ.Script.ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(3, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        await Activate<GuanYuMartialVer>();
        Assert.Null(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is ServantScriptRecord);
        using (UseCardSelector(selector)) { await Play(await AddToHand<GruntScript>(), EnemyAt(0)); }
        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Equal(3, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 4)]
    public async Task Civil_gains_temporary_dexterity_and_deals_lost_block_to_all_enemies(bool upgraded, int amount)
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<GuanDiForm>();
        if (upgraded) Upgrade(card);
        await PlayForm(card);
        Assert.Null(Player.Creature.GetPower<TempDexPower>());
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, amount);
        await CreatureCmd.GainBlock(Player.Creature, 7, ValueProp.Unpowered, null);
        var form = Player.Creature.GetPower<GuanYuCivilVerPower>()!;
        int hp = EnemyAt(0).CurrentHp;
        await form.BeforeSideTurnStart(
            new BlockingPlayerChoiceContext(), CombatSide.Enemy, [EnemyAt(0)], Combat);
        Player.Creature.LoseBlockInternal(7);
        await form.AfterBlockCleared(Player.Creature);
        Assert.Equal(hp, EnemyAt(0).CurrentHp);

        await CreatureCmd.GainBlock(Player.Creature, 7, ValueProp.Unpowered, null);
        await form.BeforeSideTurnStart(
            new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature], Combat);
        await form.AfterBlockCleared(Player.Creature);
        Assert.Equal(7, Player.Creature.Block);
        Assert.Equal(hp, EnemyAt(0).CurrentHp);

        await form.BeforeSideTurnStart(
            new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature], Combat);
        Player.Creature.LoseBlockInternal(7);
        await form.AfterBlockCleared(Player.Creature);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Equal(hp - 7, EnemyAt(0).CurrentHp);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        await Squ.Script.ScriptSystem.InvalidateScriptsAsync(Player.Creature);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Martial_grants_strength_until_deactivation_and_block_equal_to_strength_on_attacks(int amount)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await ApplyPower<GuanYuMartialVerPower>(Player.Creature, amount);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(Player.Creature, amount);
        int defendBlock = ModelDb.Card<DefendIronclad>().DynamicVars.Block.IntValue;
        await Play(await AddToHand<DefendIronclad>());
        Assert.Equal(defendBlock, Player.Creature.Block);
        var strike = await AddToHand<StrikeIronclad>();
        int strikeEnergy = strike.EnergyCost.GetResolved();
        await Play(strike, EnemyAt(0));
        Assert.Equal(defendBlock + strikeEnergy * amount, Player.Creature.Block);
        var bash = await AddToHand<Bash>();
        int bashEnergy = bash.EnergyCost.GetResolved();
        Assert.True(bashEnergy > strikeEnergy);
        int blockBeforeBash = Player.Creature.Block;
        await Play(bash, EnemyAt(0));
        Assert.Equal(blockBeforeBash + bashEnergy * amount, Player.Creature.Block);
        await Squ.Script.ScriptSystem.InvalidateScriptsAsync(Player.Creature);
        Assert.Null(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Null(Player.Creature.GetPower<StrengthPower>());
    }

    [Fact]
    public async Task Next_turn_keeps_the_script_without_a_form_choice_or_automatic_martial_bonus()
    {
        await SetupMartial();
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector)) { await EndTurn(); }
        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.NotNull(Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Null(Player.Creature.GetPower<VigorAmplificationPower>());
    }

    [Fact]
    public async Task Selection_allows_regular_scripts_plus_one_form_and_replaces_conflicting_form_on_click()
    {
        var relic = (Squ.Relics.BoxLunchRelic)ModelDb.Relic<Squ.Relics.BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<ServantScript>());
        await PlayForm(await AddToHand<GuanDiForm>());
        var records = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ScriptRecordCardTemplate>().ToArray();
        var civil = records.OfType<GuanYuCivilVer>().Single();
        var martial = records.OfType<GuanYuMartialVer>().Single();
        var servant = records.OfType<ServantScriptRecord>().Single();
        Assert.Equal((1, 2), Squ.Script.ScriptSystem.GetSelectionBounds(Player, records));
        Assert.True(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, civil]));
        Assert.True(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, martial]));
        Assert.False(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [civil, martial]));
        var anotherRegular = Combat.CreateCard<ExecutionCommanderScriptRecord>(Player);
        Assert.False(Squ.Script.ScriptSystem.IsValidActiveSelection(Player, [servant, anotherRegular, civil]));
        Assert.Same(servant, Squ.Patches.CardGridSelectionRotation.FindScriptConstraintRemoval(
            new CardModel[] { servant, civil }, anotherRegular, new CardModel[] { servant, civil }));
        Assert.Same(civil, Squ.Patches.CardGridSelectionRotation.FindScriptConstraintRemoval(
            new CardModel[] { servant, civil }, martial, new CardModel[] { servant, civil }));
        var selector = new ScriptedCardSelector().ThenSelect(servant, civil);
        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
        Assert.NotNull(Player.Creature.GetPower<GuanYuCivilVerPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.Null(Player.Creature.GetPower<GuanYuMartialVerPower>());
    }

    private async Task PlayForm(GuanDiForm card)
    {
        var selector = new ScriptedCardSelector().ThenSelect(options => options
            .Where(record => record is not GuanYuMartialVer)
            .Take(Squ.Script.ScriptSystem.GetActivationLimit(Player)).ToArray());
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
    }

    private async Task Activate<TRecord>() where TRecord : ScriptRecordCardTemplate =>
        await Squ.Script.ScriptSystem.ActivatePlayedScriptAsync(new BlockingPlayerChoiceContext(), Player,
            Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TRecord>().Single());

    private async Task SetupMartial()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await PlayForm(await AddToHand<GuanDiForm>());
        await Activate<GuanYuMartialVer>();
    }
}
