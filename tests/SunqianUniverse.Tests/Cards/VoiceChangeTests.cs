using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
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
    public async Task Upgraded_voice_change_returns_only_the_selected_exhausted_script_to_hand()
    {
        await Play(await AddToHand<ServantScript>());
        var first = await AddCardToPile<SunqianScript>(PileType.Exhaust);
        var selected = await AddCardToPile<XiliangSavageScript>(PileType.Exhaust);
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<VoiceChange>()));
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(selected, PileType.Hand);
        Assert.True(selected.IsUpgraded);
        AssertInPile(first, PileType.Exhaust);
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
    }

    [Fact]
    public async Task VoiceChange_returns_the_only_exhausted_script_and_upgrades_it()
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
