using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 《朱氏词典》：获得剧本牌时将其升级；每场战斗开始时将一张随机升级剧本牌加入手牌。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "zhu_dictionary")]
public sealed class ZhuDictionaryRelic : ScriptRelicTemplate
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/ZhuDictionaryRelic.png",
		IconOutlinePath: "res://images/relics/ZhuDictionaryRelicOutline.png",
		BigIconPath: "res://images/relics/ZhuDictionaryRelicBig.png");

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber > 1)
		{
			return;
		}

		CardModel? scriptCard = CardFactory.GetDistinctForCombat(
			Owner,
			Owner.Character.CardPool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint)
				.Where(card => IsScriptCard(card) && card.IsUpgradable),
			1,
			Owner.RunState.Rng.CombatCardGeneration)
			.FirstOrDefault();

		if (scriptCard is null)
		{
			return;
		}

		scriptCard.UpgradeInternal();
		scriptCard.FinalizeUpgradeInternal();

		Flash();
		await CardPileCmd.AddGeneratedCardToCombat(scriptCard, PileType.Hand, Owner);
	}

	public override bool TryModifyCardRewardOptionsLate(
		Player player,
		List<CardCreationResult> cardRewards,
		CardCreationOptions options)
	{
		if (player != Owner)
		{
			return false;
		}

		if (options.Flags.HasFlag(CardCreationFlags.NoHookUpgrades))
		{
			return false;
		}

		UpgradeScriptCards(cardRewards);
		return true;
	}

	public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
	{
		if (player == Owner)
		{
			UpgradeScriptCards(cards);
		}
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (card.Owner != Owner || !IsScriptCard(card) || !card.IsUpgradable)
		{
			return false;
		}

		newCard = Owner.RunState.CloneCard(card);
		CardCmd.Upgrade(newCard, CardPreviewStyle.None);
		return true;
	}

	private void UpgradeScriptCards(List<CardCreationResult> cards)
	{
		foreach (CardCreationResult entry in cards)
		{
			CardModel card = entry.Card;
			if (!IsScriptCard(card) || !card.IsUpgradable)
			{
				continue;
			}

			CardModel upgraded = Owner.RunState.CloneCard(card);
			CardCmd.Upgrade(upgraded);
			entry.ModifyCard(upgraded, this);
		}
	}

	private static bool IsScriptCard(CardModel card) => card.Tags.Contains(SquCardTags.Script);
}
