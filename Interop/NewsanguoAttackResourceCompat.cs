using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// The public Newsanguo build records Drunken Might in BeforeCardPlayed and
/// halves it afterwards. Same-owner Supreme General children inherit the root's
/// resource, so they must not create an additional consumption entry.
/// </summary>
internal static class NewsanguoAttackResourceCompat
{
    private static readonly HashSet<Type> PatchedTypes = [];

    internal static void Initialize(Harmony harmony)
    {
        if (!NewsanguoPublicApiInterop.IsReady) return;
        PowerModel? power = ModelDb.AllPowers.FirstOrDefault(NewsanguoDrunkenMightEffect.IsDrunkenMight);
        if (power is null || PatchedTypes.Contains(power.GetType())) return;

        var method = AccessTools.DeclaredMethod(power.GetType(), nameof(PowerModel.BeforeCardPlayed), [typeof(CardPlay)])
            ?? throw new MissingMethodException(power.GetType().FullName, nameof(PowerModel.BeforeCardPlayed));
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(NewsanguoAttackResourceCompat), nameof(BeforeCardPlayed)));
        PatchedTypes.Add(power.GetType());
    }

    private static bool BeforeCardPlayed(PowerModel __instance, CardPlay cardPlay, ref Task __result)
    {
        if (__instance.Owner != cardPlay.Card.Owner.Creature
            || !SupremeGeneralKeywordSystem.ShouldSuppressSameOwnerChildAttackResources(cardPlay.Card, cardPlay))
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}
