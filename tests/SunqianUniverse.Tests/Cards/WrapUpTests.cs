using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
using Squ.Patches;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WrapUpTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("wrap-up");

    [Fact]
    public async Task Wrap_deactivates_scripts_and_grants_one_energy_instead_of_extra_vigor()
    {
        await ApplyPower<ScriptOneManOneCityPower>(Player.Creature, 1);
        await PlayerCmd.SetEnergy(0, Player);

        await Play(await AddToHand<WrapUp>());

        Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            WrapUp.BaseVigor);
    }

    [Fact]
    public async Task Upgrade_grants_five_vigor()
    {
        Assert.Equal(5, WrapUp.UpgradedVigor);

        await Play(Upgrade(await AddToHand<WrapUp>()));

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            WrapUp.UpgradedVigor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Wrap_ends_only_the_clicked_script(bool endSunqian)
    {
        var boxLunch = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(boxLunch, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await Play(await AddToHand<ExecutionCommanderScript>());
        await Play(await AddToHand<NeutralAmbushScript>());
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<SunqianScript>());
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());

        foreach (var handCard in PileType.Hand.GetPile(Player).Cards.ToArray())
            await CardPileCmd.Add(handCard, PileType.Discard);
        await PlayerCmd.SetEnergy(0, Player);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card =>
            endSunqian ? card is SunqianScriptRecord : card is ServantScriptRecord);
        var top = await AddToDrawPileTop<Wound>();
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<WrapUp>());
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Equal(endSunqian, Player.Creature.GetPower<ScriptServantPower>() is not null);
        Assert.Equal(!endSunqian, Player.Creature.GetPower<ScriptSunqianPower>() is not null);
        Assert.Single(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        AssertInPile(top, endSunqian ? PileType.Hand : PileType.Draw);
        Assert.Equal(1, selector.SelectionRequestCount);
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
    }

    [Fact]
    public void Wrap_selection_completes_on_one_click_and_hides_confirmation()
    {
        var prefs = ScriptSystem.CreateWrapSelectionPrefs();
        Assert.Equal(1, prefs.MinSelect);
        Assert.Equal(1, prefs.MaxSelect);
        Assert.False(prefs.Cancelable);
        Assert.False(prefs.RequireManualConfirmation);
        Assert.True(WrapSelectionScreenPatch.HidesConfirmation(prefs));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task No_active_script_does_not_trigger_wrap_even_with_collection_records(bool hasRecord)
    {
        if (hasRecord)
            await AddCardToPile<ServantScriptRecord>(ScriptSystem.ScriptCollectionPile);
        await PlayerCmd.SetEnergy(0, Player);

        await Play(await AddToHand<WrapUp>());

        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.Equal(0, ScriptSystem.GetScriptLiftsThisTurn(Player));
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, WrapUp.BaseVigor);
    }
}
