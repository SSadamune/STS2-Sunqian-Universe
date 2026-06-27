using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「怒掀帅案」打出后授予玩家：记录本张卡牌；
/// 玩家回合开始时，若场上没有敌人处于虚弱，则将卡牌移回手牌并移除本能力。
/// 同一牌实例（<see cref="ResolveCardIdentity"/>）只会对应一个「再来撒野」。
/// </summary>
[RegisterPower]
public sealed class TroubleAgainPower : ModPowerTemplate
{
	private sealed class Data
	{
		public CardModel? TrackedCard;

		public CardModel? TrackedCardIdentity;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	public override PowerStackType StackType => PowerStackType.Single;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TroubleAgainPower.png",
		BigIconPath: "res://images/powers/TroubleAgainPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
	];

	protected override object InitInternalData() => new Data();

	public static async Task ApplyTrackingAsync(
		PlayerChoiceContext choiceContext,
		Creature owner,
		TableFlip card)
	{
		if (HasActiveTrackingForCard(owner, card))
		{
			return;
		}

		TroubleAgainPower? power = await PowerCmd.Apply<TroubleAgainPower>(
			choiceContext,
			owner,
			1,
			owner,
			card);

		power?.SetTracking(card);
	}

	public static bool HasActiveTrackingForCard(Creature owner, CardModel card) =>
		owner.GetPowerInstances<TroubleAgainPower>()
			.Any(p => p.TracksCardIdentity(ResolveCardIdentity(card)));

	public bool TracksCardIdentity(CardModel cardIdentity) =>
		GetInternalData<Data>().TrackedCardIdentity == cardIdentity;

	public void SetTracking(CardModel card)
	{
		Data data = GetInternalData<Data>();
		data.TrackedCard = card;
		data.TrackedCardIdentity = ResolveCardIdentity(card);
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

		if (AnyEnemyHasWeak(combatState))
		{
			return;
		}

		await ReturnTrackedCardAndRemoveAsync(new ThrowingPlayerChoiceContext());
	}

	private async Task ReturnTrackedCardAndRemoveAsync(PlayerChoiceContext choiceContext)
	{
		CardModel? trackedCard = GetInternalData<Data>().TrackedCard;
		if (trackedCard != null && trackedCard.Pile?.Type != PileType.Hand)
		{
			await CardPileCmd.Add(trackedCard, PileType.Hand);
		}

		await PowerCmd.Remove(this);
	}

	private static CardModel ResolveCardIdentity(CardModel card) =>
		card.DeckVersion ?? card;

	private static bool AnyEnemyHasWeak(ICombatState combatState) =>
		combatState.HittableEnemies.Any(creature =>
			creature.IsAlive && GetWeakAmount(creature) > 0);

	private static int GetWeakAmount(Creature creature) =>
		creature.GetPower<WeakPower>()?.Amount ?? 0;
}
