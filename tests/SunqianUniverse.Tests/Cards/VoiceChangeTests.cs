using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class VoiceChangeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-voice-change");
    }

    [Fact]
    public async Task VoiceChange_upgrades_and_returns_the_only_exhausted_script_to_draw_top()
    {
        var exhaustedScript = await AddCardToPile<SunqianScript>(PileType.Exhaust);
        var voiceChange = await AddToHand<VoiceChange>();

        await Play(voiceChange);

        Assert.Equal(VoiceChange.BaseBlock, Player.Creature.Block);
        Assert.True(exhaustedScript.IsUpgraded);
        AssertInPile(exhaustedScript, PileType.Draw);
        Assert.Same(exhaustedScript, PileType.Draw.GetPile(Player).Cards[0]);
    }
}
