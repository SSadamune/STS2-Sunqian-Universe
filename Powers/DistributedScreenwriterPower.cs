using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 分布式编剧：每回合开始时将 <see cref="Amount"/> 张随机剧本牌加入手牌（对齐原版创造性 AI 的叠层）。
/// 若曾打出过升级后的此牌，则生成的剧本牌一并升级。
/// </summary>
[RegisterPower]
public sealed class DistributedScreenwriterPower : ModPowerTemplate
{
	private sealed class Data
	{
		public bool GrantUpgraded;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/DistributedScreenwriterPower.png",
		BigIconPath: "res://images/powers/DistributedScreenwriterPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	protected override object InitInternalData() => new Data();

	protected override string SmartDescriptionLocKey =>
		GetInternalData<Data>().GrantUpgraded
			? base.Id.Entry + ".smartDescriptionUpgraded"
			: base.Id.Entry + ".smartDescription";

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SnapshotUpgraded(cardSource);
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (amount > 0m)
		{
			SnapshotUpgraded(cardSource);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		if (Owner.Player is not { } player || Amount <= 0m)
		{
			return;
		}

		Flash();
		bool upgraded = GetInternalData<Data>().GrantUpgraded;
		int count = (int)Amount;
		for (int i = 0; i < count; i++)
		{
			CardModel? scriptCard = CreateRandomScriptCard(player);
			if (scriptCard is null)
			{
				break;
			}

			if (upgraded)
			{
				scriptCard.UpgradeInternal();
				scriptCard.FinalizeUpgradeInternal();
			}

			await CardPileCmd.AddGeneratedCardToCombat(scriptCard, PileType.Hand, player);
		}
	}

	private void SnapshotUpgraded(CardModel? cardSource)
	{
		if (cardSource is { IsUpgraded: true })
		{
			GetInternalData<Data>().GrantUpgraded = true;
		}
	}

	private static CardModel? CreateRandomScriptCard(Player player) =>
		CardFactory.GetDistinctForCombat(
			player,
			player.Character.CardPool.GetUnlockedCards(
				player.UnlockState,
				player.RunState.CardMultiplayerConstraint)
				.Where(card => card.Tags.Contains(SquCardTags.Script)),
			1,
			player.RunState.Rng.CombatCardGeneration)
			.FirstOrDefault();
}
