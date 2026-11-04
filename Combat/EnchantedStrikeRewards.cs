#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
namespace Squ.Combat;

/// <summary>Snapshot acquisition state so later combat mutations cannot change the promised reward.</summary>
internal static class EnchantedStrikeRewards
{
    private static readonly ConditionalWeakTable<ICombatState, Dictionary<Player, List<SerializableCard>>> Battles = new();
    internal static void Initialize() => RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
    {
        if (evt.CombatState != null) Battles.Remove(evt.CombatState);
    });
    internal static void Record(CardModel card)
    {
        var data = Battles.GetOrCreateValue(card.Owner.Creature.CombatState!);
        if (!data.TryGetValue(card.Owner, out var cards)) data[card.Owner] = cards = [];
        cards.Add(card.ToSerializable());
    }
    internal static void Offer(CombatRoom room)
    {
        if (!Battles.TryGetValue(room.CombatState, out var data)) return;
        Battles.Remove(room.CombatState);
        foreach (var (player, cards) in data)
        {
            if (player.Creature.IsDead) continue;
            foreach (var snapshot in cards)
            {
                var rewardCard = CardModel.FromSerializable(snapshot);
                rewardCard.Owner = player;
                room.AddExtraReward(player, new SpecialCardReward(rewardCard, player));
            }
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class EnchantedStrikeRewardPatch
{
    [HarmonyPostfix]
    private static void Postfix(CombatRoom __2) => EnchantedStrikeRewards.Offer(__2);
}
