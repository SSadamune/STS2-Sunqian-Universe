using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 配角工牌：强怪池战斗额外遗物奖励；第三层双 Boss 的首场 Boss 战后获得升级稀有卡牌三选一。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "supporting_actor_badge")]
public sealed class SupportingActorBadgeRelic : ModRelicTemplate
{
	private const int Act3Index = 2;

	private bool _pendingUpgradedRareCardReward;

	public override RelicRarity Rarity => RelicRarity.Uncommon;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/SupportingActorBadgeRelic.png",
		IconOutlinePath: "res://images/relics/SupportingActorBadgeRelicOutline.png",
		BigIconPath: "res://images/relics/SupportingActorBadgeRelicBig.png");

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Status = ShouldPulseInRoom(room) ? RelicStatus.Active : RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (IsStrongMonsterEncounter(room))
		{
			Flash();
			room.AddExtraReward(Owner, new RelicReward(Owner));
		}

		if (IsAct3PenultimateBossFight(Owner.RunState, room))
		{
			_pendingUpgradedRareCardReward = true;
		}

		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override CardCreationOptions ModifyCardRewardCreationOptions(
		Player player,
		CardCreationOptions options)
	{
		if (player != Owner || !_pendingUpgradedRareCardReward)
		{
			return options;
		}

		return options.WithRarityOdds(CardRarityOddsType.BossEncounter);
	}

	public override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
	{
		if (player == Owner && _pendingUpgradedRareCardReward)
		{
			return 1m;
		}

		return odds;
	}

	public override bool TryModifyCardRewardOptionsLate(
		Player player,
		List<CardCreationResult> cardRewardOptions,
		CardCreationOptions creationOptions)
	{
		if (player != Owner || !_pendingUpgradedRareCardReward)
		{
			return false;
		}

		_pendingUpgradedRareCardReward = false;
		Flash();

		foreach (CardCreationResult entry in cardRewardOptions)
		{
			CardModel card = entry.Card;
			if (card.IsUpgradable && !card.IsUpgraded)
			{
				CardCmd.Upgrade(card, CardPreviewStyle.None);
			}
		}

		return true;
	}

	private bool ShouldPulseInRoom(AbstractRoom room) =>
		room is CombatRoom combat
		&& (IsStrongMonsterEncounter(combat) || IsAct3PenultimateBossFight(Owner.RunState, combat));

	private static bool IsStrongMonsterEncounter(CombatRoom room) =>
		room.RoomType == RoomType.Monster && !room.Encounter.IsWeak;

	private static bool IsAct3PenultimateBossFight(IRunState runState, CombatRoom room)
	{
		if (runState.CurrentActIndex != Act3Index || room.RoomType != RoomType.Boss)
		{
			return false;
		}

		if (!runState.Act.HasSecondBoss)
		{
			return false;
		}

		return runState.CurrentMapCoord == runState.Map.BossMapPoint.coord;
	}
}
