using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SharpenSwordForDecadesTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("sharpen-sword");

    [Fact]
    public async Task Upgraded_retention_charges_four_forge_then_play_forges_and_clears_charge()
    {
        var card = Upgrade(await AddToHand<SharpenSwordForDecades>());

        await EndTurn();

        AssertInPile(card, PileType.Hand);
        Assert.Equal(SharpenSwordForDecades.UpgradedChargeForge, card.DynamicVars.Forge.IntValue);
        await Play(card);

        var blade = Assert.Single(Player.PlayerCombatState!.AllCards.OfType<SovereignBlade>());
        Assert.Equal(10 + SharpenSwordForDecades.UpgradedChargeForge, blade.DynamicVars.Damage.IntValue);
        Assert.Equal(SharpenSwordForDecades.BaseForge, card.DynamicVars.Forge.IntValue);
    }
}
