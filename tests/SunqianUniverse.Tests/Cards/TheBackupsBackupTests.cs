using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Cards;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TheBackupsBackupTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>().WithSeed("the-backups-backup");

    [Fact]
    public async Task Card_is_multiplayer_only_zero_cost_and_upgradeable()
    {
        var card = Upgrade(await AddToHand<TheBackupsBackup>());
        Assert.True(card.IsUpgraded);
        Assert.Equal(0, card.EnergyCost.GetResolved());
        Assert.Equal(CardMultiplayerConstraint.MultiplayerOnly, card.MultiplayerConstraint);
    }
}
