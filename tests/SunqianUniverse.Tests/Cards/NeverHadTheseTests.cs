using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NeverHadTheseTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("never-had-these");

    [Fact]
    public async Task Selected_family_enters_hand_others_enter_draw_and_exhaust_triggers_energy_and_dexterity()
    {
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
        var source = Upgrade(await AddToHand<NeverHadThese>());
        using (UseCardSelector(selector))
        {
            await Play(source);
        }

        AssertAllSelectionsConsumed(selector);
        var selected = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<BelovedPartner>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<CutePet>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<NextOfKin>());
        Assert.Contains(CardKeyword.Innate, source.Keywords);
        int energy = Player.PlayerCombatState!.Energy;

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), selected);

        Assert.Equal(energy + NeverHadThese.EnergyGain, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            NeverHadThese.DexterityGain);
    }

    [Fact]
    public async Task Multiple_power_stacks_scale_both_energy_and_dexterity_on_family_exhaust()
    {
        for (int play = 0; play < 2; play++)
        {
            var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
            using (UseCardSelector(selector))
            {
                await Play(await AddToHand<NeverHadThese>());
            }
            AssertAllSelectionsConsumed(selector);
        }
        var family = PileType.Hand.GetPile(Player).Cards.OfType<BelovedPartner>().First();
        int energy = Player.PlayerCombatState!.Energy;

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), family);

        Assert.Equal(energy + 2 * NeverHadThese.EnergyGain, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            2 * NeverHadThese.DexterityGain);
    }
}
