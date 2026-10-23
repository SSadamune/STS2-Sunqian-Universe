using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HoverCardUpgradePreviewTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("hover-card-upgrade-preview");

    [Fact]
    public void Every_hover_card_that_follows_its_source_upgrade_preserves_its_upgrade_preview()
    {
        AssertUpgradePreviewIsGreen<BurningLinkedCampsScript, ShangfangguSigh>();
        AssertUpgradePreviewIsGreen<ChickenFootCheeseScript, ChickenFootCheeseScriptStrikePreview>();
        AssertUpgradePreviewIsGreen<ExecutionCommanderScript, SalvoStrike>();
        AssertUpgradePreviewIsGreen<GuanDiForm, GuanYuCivilVer>();
        AssertUpgradePreviewIsGreen<GuanDiForm, GuanYuMartialVer>();
        AssertUpgradePreviewIsGreen<NightRaidWuchaoScript, StarryNightStrike>();
        AssertUpgradePreviewIsGreen<NeutralAmbushScript, GiantRock>();
        AssertUpgradePreviewIsGreen<NeutralAmbushScript, SalvoStrike>();
        AssertUpgradePreviewIsGreen<OpenDefecation, Splash>();
        // Wine upgrades an energy-icons argument. The icon count changes, but there is no
        // textual span for the game's upgrade renderer to color green.
        AssertUpgradePreviewIsGreen<SaidNotAfraidOfAcid, Wine>(expectDescriptionHighlight: false);
        AssertUpgradePreviewIsGreen<ServantScript, ThrowHimOut>();
        AssertUpgradePreviewIsGreen<TooKindToBeTrue, UltimateDefend>();
        AssertUpgradePreviewIsGreen<WhatToEat, ExactlyWhatToEat>();

        // Slight Revision target cards use the same path, including upgrade-only text
        // such as Burn City's "two" becoming "three" despite having no upgraded DynamicVar.
        AssertUpgradePreviewIsGreen<FireNova, FireMeteorShower>();
        AssertUpgradePreviewIsGreen<NotAfraidOfAcid, SaidNotAfraidOfAcid>();
        AssertUpgradePreviewIsGreen<RelocateCapital, BurnCity>();
    }

    [Fact]
    public void Hover_card_on_an_already_upgraded_card_is_finalized_and_not_green()
    {
        NeutralAmbushScript source = Upgrade(Combat.CreateCard<NeutralAmbushScript>(Player));
        CardModel salvo = GetHoverCard<SalvoStrike>(source);

        Assert.True(salvo.IsUpgraded);
        Assert.DoesNotContain("[green]", salvo.GetDescriptionForPile(PileType.Hand));
    }

    private void AssertUpgradePreviewIsGreen<TSource, TTarget>(bool expectDescriptionHighlight = true)
        where TSource : CardModel
        where TTarget : CardModel
    {
        TSource source = Combat.CreateCard<TSource>(Player);
        source.UpgradePreviewType = CardUpgradePreviewType.Deck;
        source.UpgradeInternal();
        CardModel target = GetHoverCard<TTarget>(source);

        Assert.True(target.IsUpgraded);
        Assert.NotEqual(CardUpgradePreviewType.None, target.UpgradePreviewType);
        string description = target.GetDescriptionForUpgradePreview();
        if (expectDescriptionHighlight)
        {
            Assert.Contains("[green]", description);
        }
        else
        {
            Assert.DoesNotContain("[green]", description);
        }
    }

    private static CardModel GetHoverCard<TTarget>(CardModel source)
        where TTarget : CardModel =>
        Assert.Single(
            source.HoverTips
                .OfType<CardHoverTip>()
                .Where(tip => tip.Card is TTarget))
            .Card;
}
