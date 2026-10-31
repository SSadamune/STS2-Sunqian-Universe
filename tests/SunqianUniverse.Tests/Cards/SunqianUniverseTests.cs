using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SunqianUniverseTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("sunqian-universe-collection-growth");

    [Theory]
    [InlineData(false, 2, 2)]
    [InlineData(true, 2, 2)]
    public async Task Universe_grants_dexterity_and_draws_only_after_a_script_ends(bool upgraded, int dexterity, int draw)
    {
        await PlayerCmd.SetEnergy(20, Player);
        var card = await AddToHand<SunqianUniverse>();
        if (upgraded) Upgrade(card);
        Assert.Equal(upgraded, card.Keywords.Contains(CardKeyword.Innate));
        await Play(card);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, dexterity);
        CardTestAssertions.AssertPowerAmount<SunqianUniversePower>(Player.Creature, draw);
        await ClearHand();
        var expected = new List<CardModel>();
        for (int i = 0; i < draw; i++) expected.Add(await AddToDrawPileTop<Wound>());
        await Play(await AddToHand<ExecutionCommanderScript>());
        Assert.All(expected, drawn => AssertInPile(drawn, PileType.Draw));
        var sentinel = await AddToDrawPileTop<Wound>();
        await Play(await AddToHand<ExecutionCommanderScript>());
        AssertInPile(sentinel, PileType.Hand);
        var secondSentinel = await AddToDrawPileTop<Wound>();
        var record = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<ExecutionCommanderScriptRecord>().Single();
        await ScriptSystem.ActivatePlayedScriptAsync(new BlockingPlayerChoiceContext(), Player, record);
        AssertInPile(secondSentinel, PileType.Hand);
        var thirdSentinel = await AddToDrawPileTop<Wound>();
        await ScriptSystem.RemoveScriptPowerAsync(Player.Creature.GetPower<ScriptExecutionCommanderPower>()!);
        AssertInPile(thirdSentinel, PileType.Hand);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task Adding_or_upgrading_inactive_records_does_not_trigger_universe(bool upgraded, int draw)
    {
        await PlayerCmd.SetEnergy(20, Player);
        var card = await AddToHand<SunqianUniverse>();
        if (upgraded) Upgrade(card);
        await Play(card);
        await ClearHand();
        var expected = new List<CardModel>();
        for (int i = 0; i < draw * 2; i++) expected.Add(await AddToDrawPileTop<Wound>());
        await AddFormRecords(await AddToHand<GuanDiForm>());
        Assert.All(expected, drawn => AssertInPile(drawn, PileType.Draw));
        Assert.Equal(2, ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.Count);
        var sentinel = await AddToDrawPileTop<Wound>();
        await AddFormRecords(await AddToHand<GuanDiForm>());
        AssertInPile(sentinel, PileType.Draw);
        Assert.Equal(2, ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Normal_and_upgraded_universe_stack_to_draw_four_per_script_lift()
    {
        await PlayerCmd.SetEnergy(20, Player);
        await Play(await AddToHand<SunqianUniverse>());
        await Play(Upgrade(await AddToHand<SunqianUniverse>()));
        CardTestAssertions.AssertPowerAmount<SunqianUniversePower>(Player.Creature, 4);
        await ClearHand();
        var expected = new List<CardModel>();
        for (int i = 0; i < 4; i++) expected.Add(await AddToDrawPileTop<Wound>());
        await Play(await AddToHand<ExecutionCommanderScript>());
        Assert.All(expected, drawn => AssertInPile(drawn, PileType.Draw));
        await ScriptSystem.RemoveScriptPowerAsync(Player.Creature.GetPower<ScriptExecutionCommanderPower>()!);
        Assert.All(expected, drawn => AssertInPile(drawn, PileType.Hand));
    }

    [Fact]
    public async Task Collection_changes_do_not_trigger_universe()
    {
        await Play(await AddToHand<SunqianUniverse>());
        await ClearHand();
        var first = await AddToDrawPileTop<Wound>();
        var record = await AddCardToPile<ServantScriptRecord>(ScriptSystem.ScriptCollectionPile);
        AssertInPile(first, PileType.Draw);
        var second = await AddToDrawPileTop<Wound>();
        await CardPileCmd.Add(record, ScriptSystem.ScriptCollectionPile);
        AssertInPile(second, PileType.Draw);
        await CardPileCmd.Add(record, PileType.Discard);
        AssertInPile(second, PileType.Draw);
        await CardPileCmd.Add(record, ScriptSystem.ScriptCollectionPile);
        AssertInPile(second, PileType.Draw);
    }

    private async Task AddFormRecords(GuanDiForm source)
    {
        var context = new BlockingPlayerChoiceContext();
        await ScriptSystem.UnlockOrUpgradeAsync<GuanYuCivilVer>(context, source, 2, activate: false);
        await ScriptSystem.UnlockOrUpgradeAsync<GuanYuMartialVer>(context, source, 2, activate: false);
    }

    private async Task ClearHand()
    {
        foreach (var card in PileType.Hand.GetPile(Player).Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard);
    }
}
