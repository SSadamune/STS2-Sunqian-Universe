using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GuanDiFormTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("guan-di-form");

    [Fact]
    public async Task Upgraded_civil_choice_installs_form_and_fetches_a_free_sunqian_script()
    {
        var script = await AddCardToPile<SunqianScript>(PileType.Draw);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is GuanYuCivilVer { IsUpgraded: true });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GuanDiForm>()));
        }

        AssertAllSelectionsConsumed(selector);
        CardTestAssertions.AssertPowerAmount<GuanYuCivilVerPower>(
            Player.Creature,
            GuanYuCivilVer.BlockPerEnergy);
        AssertInPile(script, PileType.Hand);
        Assert.Equal(0, script.EnergyCost.GetResolved());
    }

    [Fact]
    public async Task Upgraded_martial_choice_converts_the_form_cards_energy_into_vigor()
    {
        await PlayerCmd.SetEnergy(3, Player);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is GuanYuMartialVer { IsUpgraded: true });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<GuanDiForm>()));
        }

        AssertAllSelectionsConsumed(selector);
        var form = Assert.IsType<GuanYuMartialVerPower>(
            Player.Creature.GetPower<GuanYuMartialVerPower>());
        Assert.Equal(GuanYuMartialVer.UpgradedVigorPerEnergy, form.Amount);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            GuanYuMartialVer.UpgradedVigorPerEnergy * 3);
        CardTestAssertions.AssertPowerAmount<VigorAmplificationPower>(
            Player.Creature,
            VigorAmplificationPower.BonusStacksPerForm);
    }
}
