using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using Squ.Cards;

#nullable enable

namespace Squ.Script;

internal static class WhatToEatDiscoverySync
{
	private static ModelId _soloId = ModelId.none;
	private static ModelId _multiId = ModelId.none;
	private static bool _idsResolved;

	private static bool TryGetPairedIds(out ModelId soloId, out ModelId multiId)
	{
		if (!_idsResolved)
		{
			try
			{
				_soloId = ModelDb.Card<WhatToEat>().Id;
				_multiId = ModelDb.Card<WhatToEatMulti>().Id;
				_idsResolved = true;
			}
			catch (Exception)
			{
				soloId = ModelId.none;
				multiId = ModelId.none;
				return false;
			}
		}

		soloId = _soloId;
		multiId = _multiId;
		return true;
	}

	public static void SyncFromSoloDiscovery(ProgressState progress)
	{
		if (!TryGetPairedIds(out ModelId soloId, out ModelId multiId))
		{
			return;
		}

		if (progress.DiscoveredCards.Contains(soloId)
			&& !progress.DiscoveredCards.Contains(multiId))
		{
			progress.MarkCardAsSeen(multiId);
		}
	}

	public static void OnCardMarkedAsSeen(ProgressState progress, ModelId cardId)
	{
		if (!TryGetPairedIds(out ModelId soloId, out ModelId multiId))
		{
			return;
		}

		if (cardId == soloId)
		{
			progress.MarkCardAsSeen(multiId);
		}
	}
}
