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
/// 「怒掀帅案」打出后授予玩家：记录被施加虚弱的敌人与本张卡牌；
/// 每个玩家回合开始时检查该敌人是否存活且已无虚弱，满足则将此牌移回手牌并移除本能力。
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
		TroubleAgainPower? power = await PowerCmd.Apply<TroubleAgainPower>(
			choiceContext,
			owner,
			1,
			owner,
			card);

		power?.SetTracking(card, weakTarget);
	}

	public void SetTracking(CardModel card, Creature weakTarget)
	{
		Data data = GetInternalData<Data>();
		data.TrackedCard = card;
		data.WeakTarget = weakTarget;
		Target = weakTarget;
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

		Data data = GetInternalData<Data>();
		Creature? weakTarget = data.WeakTarget;
		CardModel? trackedCard = data.TrackedCard;

		if (weakTarget == null || trackedCard == null)
		{
			await PowerCmd.Remove(this);
			return;
		}

		if (!weakTarget.IsAlive)
		{
			await PowerCmd.Remove(this);
			return;
		}

		if (GetWeakAmount(weakTarget) > 0)
		{
			return;
		}

		if (trackedCard.Pile?.Type != PileType.Hand)
		{
			await CardPileCmd.Add(trackedCard, PileType.Hand);
		}

		await PowerCmd.Remove(this);
	}

	private static int GetWeakAmount(Creature creature) =>
		creature.GetPower<WeakPower>()?.Amount ?? 0;
}
