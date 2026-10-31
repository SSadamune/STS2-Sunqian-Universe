using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ExecutionCommanderScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-execution-commander-script");
    }

    [Fact]
    public void ExecutionCommanderScript_upgrade_preview_highlights_seventy_five_percent()
    {
        var card = Combat.CreateCard<ExecutionCommanderScript>(Player);
        card.UpgradePreviewType = CardUpgradePreviewType.Deck;
        card.UpgradeInternal();
        var record = Assert.Single(card.HoverTips.OfType<CardHoverTip>()
            .Select(tip => tip.Card).OfType<ExecutionCommanderScriptRecord>());
        Assert.Equal(75, record.StoredAmount);
        Assert.Contains("[green]75[/green]", record.GetDescriptionForUpgradePreview());
    }

    [Fact]
    public async Task ExecutionCommanderScript_generates_two_salvos_and_applies_bonus()
    {
        var card = await AddToHand<ExecutionCommanderScript>();

        await PlayAndActivateScript(card);

        Assert.Equal(
            ExecutionCommanderScript.GeneratedStrikeCount,
            PileType.Hand.GetPile(Player).Cards.OfType<SalvoStrike>().Count());
        CardTestAssertions.AssertPowerAmount<ScriptExecutionCommanderPower>(
            Player.Creature,
            ExecutionCommanderScript.BaseBonusPercent);
        AssertOnlyScript<ScriptExecutionCommanderPower>();
    }

    [Fact]
    public async Task Upgraded_script_adds_seventy_five_percent_to_the_record()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<ExecutionCommanderScript>()));

        CardTestAssertions.AssertPowerAmount<ScriptExecutionCommanderPower>(
            Player.Creature,
            ExecutionCommanderScript.UpgradedBonusPercent);
        Assert.Equal(75, ExecutionCommanderScript.UpgradedBonusPercent);
    }
}
