#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Squ.Combat;

/// <summary>Localized markers for parameterized keyword clauses, without changing gameplay keywords.</summary>
internal static class ParameterizedKeywordRows
{
    private static readonly Func<CardModel, IEnumerable<string>>[] Providers =
        [ForgeMarkers, SelfHarmMarkers, SummonMarkers];

    internal static IEnumerable<string> LeadingMarkers(CardModel card) =>
        Providers.SelectMany(provider => provider(card));

    private static IEnumerable<string> ForgeMarkers(CardModel card)
    {
        if (card.DynamicVars.ContainsKey(ForgeVar.defaultName)
            && HoverTipFactory.Static(StaticHoverTip.Forge) is HoverTip { Title: { Length: > 0 } title })
            yield return $"[gold]{title}[/gold]";
    }

    private static IEnumerable<string> SelfHarmMarkers(CardModel card)
    {
        if (!SelfHarmSystem.HasSelfHarm(card)) yield break;
        var text = new LocString("card_keywords", "SUNQIAN_UNIVERSE_SELF_HARM.cardText");
        text.AddObj("SelfHarm", SelfHarmSystem.GetAmountVar(card));
        yield return text.GetFormattedText().TrimEnd('\r', '\n');
    }

    private static IEnumerable<string> SummonMarkers(CardModel card)
    {
        if (!card.DynamicVars.Values.OfType<SummonVar>().Any()) yield break;
        if (HoverTipFactory.Static(StaticHoverTip.SummonStatic) is HoverTip { Title: { Length: > 0 } title })
            yield return $"[gold]{title}[/gold]";
        yield return CompanionSummonKeywords.AnyCardText(card.DynamicVars.Summon).GetFormattedText();
    }
}
