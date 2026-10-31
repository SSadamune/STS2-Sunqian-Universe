using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace Squ.Combat;

/// <summary>
/// A small saved-data slot, not a hook capability. Only permanent deck entities own IDs.
/// Combat copies resolve through DeckVersion; real copies must start a fresh history.
/// Allocation occurs in synchronized lifecycle events, never in local UI queries.
/// </summary>
internal static class CardPlayRateIdentity
{
    private static readonly ModelSavedData<CardModel, Identity> Data =
        ModelSavedDataStore.For(SquMod.ModId).Register<CardModel, Identity>(
            "play_rate_id", options: new ModelSavedDataOptions
            {
                ClonePolicy = ModelSavedDataClonePolicy.Drop,
                WritePolicy = ModelSavedDataWritePolicy.AlwaysWhenPresent,
            });

    internal static void Initialize() => _ = Data;

    internal static bool TryGetKey(CardModel card, out string key)
    {
        if (Data.TryGet(card, out Identity identity) && identity.Id > 0)
        {
            key = "i" + identity.Id.ToString(CultureInfo.InvariantCulture);
            return true;
        }
        key = string.Empty;
        return false;
    }

    internal static long EnsureDeck(Player player, long nextId)
    {
        var cards = PileType.Deck.GetPile(player).Cards;
        nextId = Math.Max(1, nextId);
        // Reserve all restored IDs before assigning any missing ones, regardless of deck order.
        foreach (CardModel card in cards)
            if (Data.TryGet(card, out Identity identity) && identity.Id > 0)
                nextId = Math.Max(nextId, checked(identity.Id + 1));

        HashSet<long> seen = [];
        foreach (CardModel card in cards)
        {
            if (Data.TryGet(card, out Identity identity) && identity.Id > 0 && seen.Add(identity.Id))
                continue;
            long id = nextId;
            nextId = checked(nextId + 1);
            Data.Set(card, new Identity { Id = id });
            seen.Add(id);
        }
        return nextId;
    }

    public sealed class Identity
    {
        [JsonPropertyName("i")]
        public long Id { get; set; }
    }
}
