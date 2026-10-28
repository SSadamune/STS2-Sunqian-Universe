using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using Squ;
using Squ.Cards;
using Squ.Patches;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class ScriptSystemTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-system-rework");

    [Fact]
    public void Box_lunch_relics_reference_the_script_collection_keyword()
    {
        var expected = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(
            SquKeywords.ScriptCollectionId.GetModCardKeyword()));
        Assert.Equal("剧本集", expected.Title);
        Assert.Equal("当前战斗中，被激活过的所有剧本将记录在[gold]剧本集[/gold]。\n若剧本集中有至少四种剧本，则你的[gold]激活上限[/gold]+1。", expected.Description);
        foreach (var relic in new MegaCrit.Sts2.Core.Models.RelicModel[]
            { ModelDb.Relic<BoxLunchRelic>(), ModelDb.Relic<AbundantBoxLunchRelic>() })
        {
            Assert.Contains(relic.HoverTips.OfType<HoverTip>(), tip =>
                tip.Title == expected.Title && tip.Description == expected.Description);
        }
        var script = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(SquKeywords.Script));
        Assert.Equal("一种特殊的能力。剧本的[gold]激活上限[/gold]默认为1，因此激活新的剧本会导致旧的剧本失效。", script.Description);
    }

    [Fact]
    public async Task Abundant_box_lunch_can_reactivate_the_current_script_twice_and_pays_for_each_activation()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<ExecutionCommanderScript>();
        await PlayerCmd.SetEnergy(2, Player);
        for (int remaining = 1; remaining >= 0; remaining--)
        {
            var old = Player.Creature.GetPower<ScriptExecutionCommanderPower>()!;
            var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is ExecutionCommanderScriptRecord);
            using (UseCardSelector(selector))
            {
                Assert.True(await relic.ReselectFromScriptCollectionAsync());
            }
            AssertAllSelectionsConsumed(selector);
            var current = Player.Creature.GetPower<ScriptExecutionCommanderPower>()!;
            Assert.NotSame(old, current);
            Assert.Equal(old.Amount, current.Amount);
            Assert.Equal(remaining, Player.PlayerCombatState!.Energy);
            Assert.Equal(remaining, relic.RemainingSwitchesThisTurn);
        }
        Assert.False(relic.CanReselectFromScriptCollection());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Script_reselection_prompt_formats_the_remaining_count(int remaining)
    {
        Assert.Contains(ScriptSystem.CreateReselectionPrompt(remaining).GetFormattedText(), new[]
        {
            $"你可以重新选择要激活的剧本（本回合剩余{remaining}次）。",
            $"You may reselect Scripts to activate ({remaining} remaining this turn).",
        });
    }

    [Theory]
    [InlineData("no_relic")]
    [InlineData("spent_charge")]
    [InlineData("no_energy")]
    public async Task Script_collection_view_shows_the_read_only_footer(string reason)
    {
        await PlayScript<ExecutionCommanderScript>();
        if (reason != "no_relic")
        {
            BoxLunchRelic relic = reason == "no_energy"
                ? (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable()
                : (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
            Player.AddRelicInternal(relic, silent: true);
            if (reason == "no_energy")
            {
                await PlayerCmd.SetEnergy(0, Player);
                Assert.Equal(2, relic.RemainingSwitchesThisTurn);
            }
            else
            {
                relic.MarkSwitchesUsed(Player.PlayerCombatState!.TurnNumber, 1);
                Assert.Equal(0, relic.RemainingSwitchesThisTurn);
            }
            Assert.False(relic.CanReselectFromScriptCollection());
        }

        NCardPileScreen screen = NCardPileScreen.ShowScreen(
            ScriptSystem.ScriptCollectionPile.GetPile(Player), []);
        try
        {
            var label = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
            Assert.True(label.Visible);
            Assert.Contains(label.Text, new[]
            {
                "[center]查看本场战斗中激活过的剧本。",
                "[center]View Scripts activated during this combat.",
            });
        }
        finally
        {
            NCapstoneContainer.Instance!.Close();
        }
    }

    [Fact]
    public void Script_collection_hover_explains_history_and_the_four_script_threshold()
    {
        string description = new LocString("static_hover_tips",
            "SUNQIAN_UNIVERSE_CARDPILE_SCRIPT_COLLECTION.description").GetFormattedText();
        Assert.Contains(description, new[]
        {
            "点击查看本场战斗中激活过、以及正在激活的剧本。\n若剧本集中有至少四种剧本，则你的[gold]激活上限[/gold]+1。",
            "Click to view Scripts previously activated during this combat and those currently active.\nIf the Script Collection contains at least four kinds of Script, your [gold]activation limit[/gold] +1.",
        });
    }

    [Fact]
    public async Task Playing_a_new_script_auto_activates_it_and_replaces_the_only_active_script()
    {
        await PlayScript<NeutralAmbushScript>();
        NeutralAmbushScriptRecord neutralRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<NeutralAmbushScriptRecord>());
        Assert.True(neutralRecord.ShouldGlowGold);

        await PlayScript<ServantScript>();

        CardModel[] records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.ToArray();
        Assert.Contains(records, card => card is NeutralAmbushScriptRecord);
        Assert.Contains(records, card => card is ServantScriptRecord);
        Assert.Null(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.False(neutralRecord.ShouldGlowGold);
        Assert.True(Assert.Single(records.OfType<ServantScriptRecord>()).ShouldGlowGold);
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Playing_a_reinforceable_script_again_strengthens_one_existing_record()
    {
        await PlayScript<ExecutionCommanderScript>();
        var firstPower = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());

        await PlayScript<ExecutionCommanderScript>();

        ExecutionCommanderScriptRecord record = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<ExecutionCommanderScriptRecord>());
        Assert.Equal(ExecutionCommanderScript.BaseBonusPercent * 2, record.StoredAmount);
        var reinforcedPower = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.NotSame(firstPower, reinforcedPower);
        Assert.Equal(record.StoredAmount, reinforcedPower.Amount);
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
    }

    [Fact]
    public async Task Sunqian_draws_on_play_and_its_temporary_dexterity_survives_switching_out()
    {
        var drawn = await AddToDrawPileTop<DefendIronclad>();
        await PlayScript<SunqianScript>();
        AssertInPile(drawn, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        var staysInDrawPile = await AddToDrawPileTop<StrikeIronclad>();

        await PlayScript<ServantScript>();

        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.BaseDexterity);
        AssertInPile(staysInDrawPile, PileType.Draw);
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Upgraded_sunqian_draws_two_and_grants_three_temporary_dexterity()
    {
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();

        await Play(Upgrade(await AddToHand<SunqianScript>()));

        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<TempDexPower>(
            Player.Creature,
            SunqianScript.UpgradedDexterity);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            SunqianScript.UpgradedDexterity);
    }

    [Fact]
    public async Task Replaying_the_active_eunuch_rebuilds_the_power_and_both_plays_keep_their_drawn_cards()
    {
        CardModel first = await AddToDrawPileTop<StrikeIronclad>();
        CardModel second = await AddToDrawPileTop<DefendIronclad>();
        await PlayScript<EunuchScript>();
        var active = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel third = await AddToDrawPileTop<Bash>();
        CardModel fourth = await AddToDrawPileTop<Wound>();
        await PlayScript<EunuchScript>();

        Assert.NotSame(active, Player.Creature.GetPower<ScriptEunuchPower>());
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        AssertInPile(third, PileType.Hand);
        AssertInPile(fourth, PileType.Hand);
        Assert.True(first.HasEunuchMessage());
        Assert.True(second.HasEunuchMessage());
        Assert.True(third.HasEunuchMessage());
        Assert.True(fourth.HasEunuchMessage());
    }

    [Fact]
    public async Task Box_lunch_reactivates_the_same_script_and_spends_the_charge()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        CardModel oldSecond = await AddToDrawPileTop<DefendIronclad>();
        CardModel oldFirst = await AddToDrawPileTop<StrikeIronclad>();
        await PlayScript<EunuchScript>();
        var oldPower = Assert.IsType<ScriptEunuchPower>(
            Player.Creature.GetPower<ScriptEunuchPower>());

        CardModel newSecond = await AddToDrawPileTop<Wound>();
        CardModel newFirst = await AddToDrawPileTop<Bash>();
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is EunuchScriptRecord);
        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
            Assert.False(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);

        Assert.NotSame(oldPower, Player.Creature.GetPower<ScriptEunuchPower>());
        Assert.Equal(1, ScriptSystem.GetScriptLiftsThisTurn(Player));
        AssertInPile(oldFirst, PileType.Hand);
        AssertInPile(oldSecond, PileType.Hand);
        AssertInPile(newFirst, PileType.Draw);
        AssertInPile(newSecond, PileType.Draw);
        Assert.True(oldFirst.HasEunuchMessage());
        Assert.True(oldSecond.HasEunuchMessage());
        Assert.False(newFirst.HasEunuchMessage());
        Assert.False(newSecond.HasEunuchMessage());
        Assert.False(relic.CanReselectFromScriptCollection());
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
    }

    [Fact]
    public async Task Box_lunch_requires_one_selection_when_two_scripts_are_available()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<SunqianScript>();
        await PlayScript<ExecutionCommanderScript>();
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is SunqianScriptRecord);

        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(1, selector.SelectionRequestCount);
        Assert.Equal(1, selector.LastMinSelect);
        Assert.Equal(1, selector.LastMaxSelect);
        AssertOnlyScript<ScriptSunqianPower>();
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
        Assert.False(relic.CanReselectFromScriptCollection());

        await EndTurn();

        Assert.Equal(1, relic.RemainingSwitchesThisTurn);
        Assert.True(relic.CanReselectFromScriptCollection());
    }

    [Fact]
    public async Task Box_lunch_cancel_keeps_the_active_script_and_does_not_spend_the_turns_reselection()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<SunqianScript>();
        await PlayScript<ExecutionCommanderScript>();
        var original = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());

        var cancelSelector = new ScriptedCardSelector().ThenSkip();
        using (UseCardSelector(cancelSelector))
        {
            Assert.False(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(cancelSelector);
        Assert.Same(original, Player.Creature.GetPower<ScriptExecutionCommanderPower>());

        var confirmSelector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is SunqianScriptRecord);
        using (UseCardSelector(confirmSelector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(confirmSelector);
        AssertOnlyScript<ScriptSunqianPower>();
    }

    [Fact]
    public async Task Cancelable_script_selection_adds_the_native_back_button_and_returns_an_empty_choice()
    {
        await PlayScript<SunqianScript>();
        await PlayScript<ExecutionCommanderScript>();
        var prefs = new CardSelectorPrefs(
            ScriptSystem.CreateReselectionPrompt(2),
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
            PretendCardsCanBePlayed = true,
        };
        NCombatPileCardSelectScreen screen = NCombatPileCardSelectScreen.Create(
            ScriptSystem.ScriptCollectionPile.GetPile(Player),
            prefs,
            card => card is ScriptRecordCardTemplate);
        NOverlayStack.Instance!.Push(screen);
        await Engine.GetMainLoop().ToSignal(
            Engine.GetMainLoop(),
            SceneTree.SignalName.ProcessFrame);

        NBackButton backButton = screen.GetNode<NBackButton>("ScriptSelectionBackButton");
        Assert.True(CardGridSelectionRotation.UsesRotation(prefs));
        Assert.Equal(prefs.Prompt.GetFormattedText(),
            screen.GetNode<Control>("%BottomText").GetNode<MegaRichTextLabel>("%BottomLabel").Text);
        Assert.Contains(prefs.Prompt.GetFormattedText(), new[]
        {
            "你可以重新选择要激活的剧本（本回合剩余2次）。",
            "You may reselect Scripts to activate (2 remaining this turn).",
        });
        Task<IEnumerable<CardModel>> selectedTask = screen.CardsSelected();
        backButton.EmitSignal(NClickableControl.SignalName.Released, backButton);

        Assert.Empty(await selectedTask);
    }

    [Fact]
    public async Task Box_lunch_reactivates_the_same_reinforceable_script_without_stacking_its_values()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<ExecutionCommanderScript>();
        var active = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is ExecutionCommanderScriptRecord);

        using (UseCardSelector(selector))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(selector);

        var reactivated = Assert.IsType<ScriptExecutionCommanderPower>(
            Player.Creature.GetPower<ScriptExecutionCommanderPower>());
        Assert.NotSame(active, reactivated);
        Assert.Equal(active.Amount, reactivated.Amount);
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
        Assert.False(relic.CanReselectFromScriptCollection());
    }

    [Fact]
    public async Task Script_collection_adds_exactly_one_slot_once_four_scripts_are_unlocked_without_a_relic()
    {
        await PlayerCmd.SetEnergy(0, Player);

        await PlayScript<ExecutionCommanderScript>();
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.Equal(1, ScriptSystem.GetActivationLimit(Player));

        await PlayerCmd.SetEnergy(10, Player);
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        await PlayScript<SunqianScript>();

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
    }

    [Fact]
    public async Task Abundant_box_lunch_gains_one_energy_at_turn_start_without_opening_selection()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayScript<ExecutionCommanderScript>();
        await PlayerCmd.SetEnergy(0, Player);

        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector))
        {
            await relic.AfterSideTurnStart(
                CombatSide.Player,
                [Player.Creature],
                Combat);
        }

        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.Equal(AbundantBoxLunchRelic.TurnStartEnergy, Player.PlayerCombatState!.Energy);
    }

    [Fact]
    public async Task Abundant_box_lunch_pays_one_energy_per_real_switch_and_stops_without_energy_or_charges()
    {
        var relic = (AbundantBoxLunchRelic)ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);
        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<SunqianScript>();
        await PlayerCmd.SetEnergy(2, Player);

        Assert.Equal(2, Player.PlayerCombatState!.Energy);
        Assert.Equal(2, relic.RemainingSwitchesThisTurn);
        AssertOnlyScript<ScriptSunqianPower>();

        var cancel = new ScriptedCardSelector().ThenSkip();
        using (UseCardSelector(cancel))
        {
            Assert.False(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(cancel);
        Assert.Equal(2, Player.PlayerCombatState.Energy);
        Assert.True(relic.CanReselectFromScriptCollection());

        var toCommander = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is ExecutionCommanderScriptRecord);
        using (UseCardSelector(toCommander))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(toCommander);
        Assert.Equal(1, Player.PlayerCombatState.Energy);
        Assert.Equal(1, relic.RemainingSwitchesThisTurn);
        AssertOnlyScript<ScriptExecutionCommanderPower>();

        var backToSunqian = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is SunqianScriptRecord);
        using (UseCardSelector(backToSunqian))
        {
            Assert.True(await relic.ReselectFromScriptCollectionAsync());
        }
        AssertAllSelectionsConsumed(backToSunqian);
        Assert.Equal(0, Player.PlayerCombatState.Energy);
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
        AssertOnlyScript<ScriptSunqianPower>();
        Assert.False(relic.CanReselectFromScriptCollection());
        Assert.False(await relic.ReselectFromScriptCollectionAsync());

        await PlayerCmd.SetEnergy(5, Player);
        Assert.False(relic.CanReselectFromScriptCollection());
        Assert.False(await relic.ReselectFromScriptCollectionAsync());
        AssertOnlyScript<ScriptSunqianPower>();
    }

    [Fact]
    public void Script_activation_selection_replaces_the_oldest_choice_when_full()
    {
        CardModel a = ModelDb.Card<SunqianScriptRecord>();
        CardModel b = ModelDb.Card<ServantScriptRecord>();
        CardModel c = ModelDb.Card<NeutralAmbushScriptRecord>();
        CardModel d = ModelDb.Card<GruntScriptRecord>();
        var selected = new HashSet<CardModel>();
        var order = new ScriptActivationSelectionOrder();

        Click(b);
        Click(c);
        Click(a);
        Click(d);

        Assert.DoesNotContain(b, selected);
        Assert.Equal([c, a, d], order.Cards);

        Assert.True(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            SquCommonL10n.Table,
            ScriptSystem.SelectionPromptKey)));
        Assert.True(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            SquCommonL10n.Table,
            ScriptSystem.ReselectionPromptKey)));
        Assert.True(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            "cards",
            FarmingGeneral.SelectionPromptKey)));
        Assert.True(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            "cards",
            GoldenUprising.SelectionPromptKey)));
        Assert.False(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            SquCommonL10n.Table,
            "SUNQIAN_UNIVERSE_COMMON.SCRIPT.invalidationPrompt")));
        Assert.False(CardGridSelectionRotation.UsesRotation(SelectionPrefs(
            "powers",
            "SUNQIAN_UNIVERSE_POWER_SCRIPT_PART_TIME_GENERAL_POWER.selectionScreenPrompt")));

        void Click(CardModel card)
        {
            CardModel? removed = order.MakeRoomFor(selected, card, maxSelect: 3);
            if (removed is not null)
            {
                selected.Remove(removed);
            }

            if (!selected.Remove(card))
            {
                selected.Add(card);
            }
            order.RecordResult(selected, card);
        }

        static CardSelectorPrefs SelectionPrefs(string table, string key) =>
            new(new MegaCrit.Sts2.Core.Localization.LocString(table, key), 2);
    }

    [Fact]
    public void Selected_script_highlight_blue_takes_priority_over_active_white()
    {
        Assert.Equal(
            ScriptRecordCardHighlight.ActiveColor,
            ScriptRecordCardHighlight.ResolveColor(isActive: true, isSelected: false));
        Assert.Equal(
            NCardHighlight.playableColor,
            ScriptRecordCardHighlight.ResolveColor(isActive: true, isSelected: true));
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.R);
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.G);
        Assert.Equal(1f, ScriptRecordCardHighlight.ActiveColor.B);
        Assert.True(ScriptRecordCardHighlight.KeepVisibleOnHide(isActive: true, isSelected: false));
        Assert.False(ScriptRecordCardHighlight.KeepVisibleOnHide(isActive: true, isSelected: true));
        Assert.False(ScriptRecordCardHighlight.KeepVisibleOnHide(isActive: false, isSelected: false));
    }

    [Fact]
    public async Task Playing_a_new_script_with_multiple_active_scripts_asks_which_old_script_ends()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await PlayScript<ExecutionCommanderScript>();
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        await PlayScript<SunqianScript>();

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxAdvanceScriptRecord)
            .ThenSelectWhere(card => card is ServantScriptRecord);
        using (UseCardSelector(selector))
        {
            await PlayScript<BowangParadoxScript>();
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.Null(Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxAdvancePower>());
    }

    [Fact]
    public async Task Active_record_highlight_tracks_manual_reselection()
    {
        await PlayScript<NeutralAmbushScript>();
        await PlayScript<ServantScript>();
        NeutralAmbushScriptRecord neutralRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<NeutralAmbushScriptRecord>());
        ServantScriptRecord servantRecord = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<ServantScriptRecord>());
        Assert.False(neutralRecord.ShouldGlowGold);
        Assert.True(servantRecord.ShouldGlowGold);

        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is NeutralAmbushScriptRecord);
        using (UseCardSelector(selector))
        {
            bool selected = await ScriptSystem.ChooseActiveScriptsAsync(
                new BlockingPlayerChoiceContext(),
                Player,
                cancelable: false);
            Assert.True(selected);
        }
        AssertAllSelectionsConsumed(selector);

        Assert.True(neutralRecord.ShouldGlowGold);
        Assert.False(servantRecord.ShouldGlowGold);
    }

    [Fact]
    public async Task Manual_reselection_passes_its_choice_context_to_bowang_paradox()
    {
        var initialSelector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxAdvanceScriptRecord);
        using (UseCardSelector(initialSelector))
        {
            await PlayScript<BowangParadoxScript>();
        }
        AssertAllSelectionsConsumed(initialSelector);

        CardModel burning = await AddToHand<BlockFirefighting>();
        var reselectSelector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is BowangParadoxArsonScriptRecord);
        using (UseCardSelector(reselectSelector))
        {
            bool selected = await ScriptSystem.ChooseActiveScriptsAsync(
                new BlockingPlayerChoiceContext(),
                Player,
                cancelable: false);
            Assert.True(selected);
        }
        AssertAllSelectionsConsumed(reselectSelector);

        AssertInPile(burning, PileType.Discard);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BowangParadoxScript.BaseTinderStacks);
        AssertOnlyScript<ScriptBowangParadoxArsonPower>();
    }

    private async Task PlayScript<TScript>()
        where TScript : ScriptCardTemplate
        => await Play(await AddToHand<TScript>());
}
