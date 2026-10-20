using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NeutralAmbushScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("neutral-ambush-script");

    [Fact]
    public async Task Generates_upgraded_rocks_and_salvos_and_becomes_active_script()
    {
        await Play(Upgrade(await AddToHand<NeutralAmbushScript>()));

        var rocks = PileType.Draw.GetPile(Player).Cards.OfType<GiantRock>().ToArray();
        var salvos = PileType.Draw.GetPile(Player).Cards.OfType<SalvoStrike>().ToArray();
        Assert.Equal(NeutralAmbushScript.BoulderCount, rocks.Length);
        Assert.Equal(NeutralAmbushScript.SalvoStrikeCount, salvos.Length);
        Assert.All(rocks.Cast<CardModel>().Concat(salvos.Cast<CardModel>()), card => Assert.True(card.IsUpgraded));
        AssertOnlyScript<ScriptNeutralAmbushPower>();
    }

    [Fact]
    public async Task Only_the_first_matching_card_drawn_each_turn_is_auto_played()
    {
        await ApplyPower<ScriptNeutralAmbushPower>(Player.Creature, 1);
        var power = Assert.IsType<ScriptNeutralAmbushPower>(
            Player.Creature.GetPower<ScriptNeutralAmbushPower>());
        var rock = await AddToDrawPileTop<GiantRock>();
        var firstSalvo = await AddToDrawPileTop<SalvoStrike>();

        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 2, Player);

        Assert.Equal(PileType.Exhaust, firstSalvo.Pile?.Type);
        Assert.Equal(PileType.Hand, rock.Pile?.Type);

        await power.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Combat);
        var nextTurnSalvo = await AddToDrawPileTop<SalvoStrike>();

        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player);

        Assert.Equal(PileType.Exhaust, nextTurnSalvo.Pile?.Type);
    }
}
