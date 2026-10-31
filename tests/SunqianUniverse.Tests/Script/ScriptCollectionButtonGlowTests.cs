using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Patches;
using Squ.Relics;
using Squ.Script;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardPiles.Nodes;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Script;

public sealed class ScriptCollectionButtonGlowTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("script-collection-button-glow");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Purple_glow_tracks_reactivation_uses_energy_and_cooldown(bool abundant)
    {
        var relic = (BoxLunchRelic)(abundant
            ? ModelDb.Relic<AbundantBoxLunchRelic>().ToMutable()
            : ModelDb.Relic<BoxLunchRelic>().ToMutable());
        Player.AddRelicInternal(relic, silent: true);
        var button = NModCardPileButton.Create(ModCardPileRegistry.Get(ScriptSystem.ScriptCollectionPile));
        button.Initialize(Player);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(button);
        try
        {
            await Frame();
            var glow = button.GetNode<TextureRect>("Control/Icon/" + ScriptCollectionButtonGlowPatch.GlowNodeName);
            Assert.False(glow.Visible); // No script has been unlocked yet.
            Assert.Equal(Control.MouseFilterEnum.Ignore, glow.MouseFilter);
            Assert.True(glow.ShowBehindParent);
            Assert.IsType<ShaderMaterial>(glow.Material);

            await Play(await AddToHand<ExecutionCommanderScript>());
            await PlayerCmd.SetEnergy(1, Player);
            await Frame();
            Assert.True(glow.Visible);

            if (abundant)
            {
                await PlayerCmd.SetEnergy(0, Player);
                await Frame();
                Assert.False(glow.Visible);
                await PlayerCmd.SetEnergy(1, Player);
                await Frame();
                Assert.True(glow.Visible);
            }

            var cancel = new ScriptedCardSelector().ThenSkip();
            using (UseCardSelector(cancel))
                Assert.False(await relic.ReselectFromScriptCollectionAsync());
            AssertAllSelectionsConsumed(cancel);
            await Frame();
            Assert.True(glow.Visible);

            var select = new ScriptedCardSelector().ThenSelectFirst();
            using (UseCardSelector(select))
                Assert.True(await relic.ReselectFromScriptCollectionAsync());
            AssertAllSelectionsConsumed(select);
            await Frame();
            Assert.False(glow.Visible);

            await EndTurn();
            await Frame();
            Assert.Equal(abundant, glow.Visible);
            if (!abundant)
            {
                await EndTurn();
                await Frame();
                Assert.True(glow.Visible);
            }
            Assert.Single(button.GetNode("Control/Icon").GetChildren().OfType<TextureRect>());
        }
        finally
        {
            tree.Root.RemoveChild(button);
            button.QueueFree();
        }
    }

    private static async Task Frame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
