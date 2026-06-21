using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
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
/// 「怒掀帅案」打出后授予玩家：记录被施加虚弱的敌人与本张卡牌；
/// 实时监测目标死亡或虚弱消失，满足条件时移回卡牌并移除本能力。
/// </summary>
[RegisterPower]
public sealed class TroubleAgainPower : ModPowerTemplate
{
	private sealed class Data
	{
		public CardModel? TrackedCard;

		public Creature? WeakTarget;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	public override PowerStackType StackType => PowerStackType.Single;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TroubleAgainPower.png",
		BigIconPath: "res://images/powers/TroubleAgainPower.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
	];

	protected override object InitInternalData() => new Data();

	public static async Task ApplyTrackingAsync(
		PlayerChoiceContext choiceContext,
		Creature owner,
		TableFlip card,
		Creature weakTarget)
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

		power?.SetTracking(card, weakTarget);
	}

	public static bool HasActiveTrackingForCard(Creature owner, CardModel card) =>
		owner.GetPowerInstances<TroubleAgainPower>().Any(p => p.TracksCard(card));

	public bool TracksCard(CardModel card) =>
		GetInternalData<Data>().TrackedCard == card;

	public void SetTracking(CardModel card, Creature weakTarget)
	{
		Data data = GetInternalData<Data>();
		data.TrackedCard = card;
		data.WeakTarget = weakTarget;
		Target = weakTarget;
	}

	public override async Task AfterDeath(
		PlayerChoiceContext choiceContext,
		Creature creature,
		bool wasRemovalPrevented,
		float deathAnimLength)
	{
		if (wasRemovalPrevented || creature != GetInternalData<Data>().WeakTarget)
		{
			return;
		}

		await PowerCmd.Remove(this);
	}

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power is not WeakPower || power.Owner != GetInternalData<Data>().WeakTarget)
		{
			return;
		}

		if (GetWeakAmount(power.Owner) > 0)
		{
			return;
		}

		await ReturnTrackedCardAndRemoveAsync(choiceContext);
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

	private static int GetWeakAmount(Creature creature) =>
		creature.GetPower<WeakPower>()?.Amount ?? 0;
}
