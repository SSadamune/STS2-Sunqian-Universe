#nullable enable
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
namespace Squ.Combat;

internal static class BowangParadoxEffects
{
    internal static async Task Apply(PlayerChoiceContext context, Player player, decimal amount, bool arson, CardModel? source)
    {
        if (arson) await PowerCmd.Apply<TinderPower>(context, player.Creature, amount, player.Creature, source);
        else await PowerCmd.Apply<VigorPower>(context, player.Creature, amount, player.Creature, source);
        var discarded = PileType.Hand.GetPile(player).Cards.Where(card => !card.ShouldRetainThisTurn
            && (arson ? SquCardTags.AppliesBurning(card) : card.Type == CardType.Attack)).ToList();
        if (discarded.Count > 0) await CardCmd.DiscardAndDraw(context, discarded, 0);
    }
}
