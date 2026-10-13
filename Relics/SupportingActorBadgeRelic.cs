using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 配角工牌：强怪遭遇战使用精英战奖励；第三阶段双 Boss 的首战后获得稀有药水与稀有卡牌奖励。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "supporting_actor_badge")]
public sealed class SupportingActorBadgeRelic : ModRelicTemplate
{
	private const int Act3Index = 2;

	public override RelicRarity Rarity => RelicRarity.Uncommon;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/SupportingActorBadgeRelic.png",
		IconOutlinePath: "res://images/relics/SupportingActorBadgeRelicOutline.png",
		BigIconPath: "res://images/relics/SupportingActorBadgeRelicBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.StrongMonsterEncounter),
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Status = ShouldPulseInRoom(room) ? RelicStatus.Active : RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override bool TryModifyRewards(
		Player player,
		List<Reward> rewards,
		AbstractRoom? room)
	{
		if (player != Owner
			|| room is not CombatRoom combatRoom
			|| !IsAct3PenultimateBossFight(player.RunState, combatRoom))
		{
			return false;
		}

		Flash();
		rewards.Add(CreateRarePotionReward(player));
		if (!rewards.Any(reward => reward is CardReward))
		{
			rewards.Add(new CardReward(
				CardCreationOptions.ForRoom(player, RoomType.Boss)
					.WithFlags(CardCreationFlags.IsFromCombat),
				3,
				player));
		}

		return true;
	}

	private static PotionReward CreateRarePotionReward(Player player)
	{
		IEnumerable<PotionModel> rarePotions = PotionFactory.GetPotionOptions(player)
			.Where(potion => potion.Rarity == PotionRarity.Rare);
		PotionModel potion = player.PlayerRng.Rewards.NextItem(rarePotions)
			?? throw new InvalidOperationException("No unlocked rare potion is available for 配角工牌.");
		return new PotionReward(potion.ToMutable(), player);
	}

	private bool ShouldPulseInRoom(AbstractRoom room) =>
		room is CombatRoom combat
		&& (IsStrongMonsterEncounter(combat) || IsAct3PenultimateBossFight(Owner.RunState, combat));

	internal static bool IsStrongMonsterEncounter(CombatRoom room) =>
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
