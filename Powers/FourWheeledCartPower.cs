using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// Amount represents the number of carts. A qualifying hit queues one layer for transfer, delayed
/// until the source Attack card or monster move has completely resolved. The same resolution can
/// take one layer from each damaged owner, but never more than one layer from the same owner.
/// </summary>
[RegisterPower]
public sealed class FourWheeledCartPower : ModPowerTemplate
{
	private const int PlayerEnergyGain = 1;
	private const string GrantedStrengthVarName = "GrantedStrength";
	private const string GrantedDexterityVarName = "GrantedDexterity";

	private sealed class GrantedStrengthVar()
		: DynamicVar(GrantedStrengthVarName, 0)
	{
		private decimal CurrentValue =>
			_owner is FourWheeledCartPower power ? power.GrantedStrength : 0;

		protected override decimal GetBaseValueForIConvertible() => CurrentValue;

		public override string ToString() =>
			((int)CurrentValue).ToString(CultureInfo.InvariantCulture);
	}

	private sealed class GrantedDexterityVar()
		: DynamicVar(GrantedDexterityVarName, 0)
	{
		private decimal CurrentValue =>
			_owner is FourWheeledCartPower power ? power.GrantedDexterity : 0;

		protected override decimal GetBaseValueForIConvertible() => CurrentValue;

		public override string ToString() =>
			((int)CurrentValue).ToString(CultureInfo.InvariantCulture);
	}

	private sealed record PendingTransfer(Creature Dealer, CardModel? CardSource);

	private static readonly List<FourWheeledCartPower> PendingPowers = [];

	private List<PendingTransfer>? _pendingTransfers;
	private bool _isRemovingForTransfer;

	[SavedProperty]
	private int GrantedStrength { get; set; }

	[SavedProperty]
	private int GrantedDexterity { get; set; }

	public override PowerType Type => PowerType.Buff;

	public override PowerInstanceType InstanceType => PowerInstanceType.None;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/FourWheeledCartPower.png",
		BigIconPath: "res://images/powers/FourWheeledCartPowerBig.png");

	protected override string SmartDescriptionLocKey =>
		Amount >= 2
			? Id.Entry + ".smartDescriptionMulti"
			: base.SmartDescriptionLocKey;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GrantedStrengthVar(),
		new GrantedDexterityVar(),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power != this || (int)amount == 0)
		{
			return;
		}

		await ApplyLayerStatDelta((int)amount, applier, cardSource);
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		PendingPowers.Remove(this);
		_pendingTransfers?.Clear();
		await RemoveGrantedStat<StrengthPower>(oldOwner, GrantedStrength);
		await RemoveGrantedStat<DexterityPower>(oldOwner, GrantedDexterity);
		GrantedStrength = 0;
		GrantedDexterity = 0;
	}

	public override Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (result.Receiver == Owner
			&& dealer is not null
			&& ResolveTransferRecipient(dealer) != Owner
			&& result.UnblockedDamage > 0
			&& props.IsPoweredAttack()
			&& !HasPendingTransfer(dealer, cardSource))
		{
			PendingTransferList.Add(new PendingTransfer(dealer, cardSource));
			if (!PendingPowers.Contains(this))
			{
				PendingPowers.Add(this);
			}
			Flash();
		}

		return Task.CompletedTask;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

	public override bool ShouldOwnerDeathTriggerFatal() => false;

	private List<PendingTransfer> PendingTransferList =>
		_pendingTransfers ??= [];

	private bool HasPendingTransfer(Creature dealer, CardModel? cardSource) =>
		PendingTransferList.Any(pending =>
			cardSource is not null
				? ReferenceEquals(pending.CardSource, cardSource)
				: pending.CardSource is null
					&& ReferenceEquals(pending.Dealer, dealer));

	private async Task ApplyLayerStatDelta(
		int layerDelta,
		Creature? applier,
		CardModel? cardSource)
	{
		GrantedStrength += await ApplyGrantedStat<StrengthPower>(
			Owner,
			GiftFourWheeledCart.StrengthAmount * layerDelta,
			applier,
			cardSource);
		GrantedDexterity += await ApplyGrantedStat<DexterityPower>(
			Owner,
			GiftFourWheeledCart.DexterityAmount * layerDelta,
			applier,
			cardSource);
	}

	private static async Task RemoveGrantedStat<T>(Creature owner, int grantedAmount)
		where T : PowerModel
	{
		if (grantedAmount == 0 || owner.GetPower<T>() is not { } power)
		{
			return;
		}

		power.SetAmount(power.Amount - grantedAmount);
		if (power.ShouldRemoveDueToAmount())
		{
			await PowerCmd.Remove(power);
		}
	}

	private static async Task<int> ApplyGrantedStat<T>(
		Creature owner,
		int amount,
		Creature? applier,
		CardModel? cardSource)
		where T : PowerModel
	{
		int before = owner.GetPowerAmount<T>();
		T? applied = await PowerCmd.Apply<T>(
			new ThrowingPlayerChoiceContext(),
			owner,
			amount,
			applier,
			cardSource);
		if (applied is null
			&& CombatManager.Instance.IsEnding
			&& owner.CombatState is not null
			&& owner.CanReceivePowers)
		{
			if (owner.GetPower<T>() is { } existing)
			{
				existing.SetAmount(existing.Amount + amount);
				if (existing.ShouldRemoveDueToAmount())
				{
					await PowerCmd.Remove(existing);
				}
			}
			else
			{
				var created = (T)ModelDb.Power<T>().ToMutable();
				created.Applier = applier;
				created.ApplyInternal(owner, amount);
				await created.AfterApplied(applier, cardSource);
			}
		}

		return owner.GetPowerAmount<T>() - before;
	}

	private async Task TransferTo(PlayerChoiceContext choiceContext, Creature dealer)
	{
		Creature recipient = ResolveTransferRecipient(dealer);
		if (_isRemovingForTransfer || recipient == Owner || recipient.CombatState is null)
		{
			return;
		}

		_isRemovingForTransfer = true;
		try
		{
			if (!await RemoveOneLayer(choiceContext, dealer))
			{
				return;
			}

			FourWheeledCartPower? transferred = await PowerCmd.Apply<FourWheeledCartPower>(
				choiceContext,
				recipient,
				1m,
				dealer,
				null);
			if (transferred is null
				&& CombatManager.Instance.IsEnding
				&& recipient.CanReceivePowers)
			{
				transferred = await ApplyOneLayerDuringCombatEnd(recipient, dealer);
			}

			if (transferred is not null && recipient.Player is { } player)
			{
				await PlayerCmd.GainEnergy(PlayerEnergyGain, player);
			}
		}
		finally
		{
			_isRemovingForTransfer = false;
		}
	}

	private async Task<bool> RemoveOneLayer(
		PlayerChoiceContext choiceContext,
		Creature dealer)
	{
		int previousAmount = Amount;
		if (previousAmount <= 0)
		{
			return false;
		}

		if (!CombatManager.Instance.IsEnding)
		{
			await PowerCmd.ModifyAmount(
				choiceContext,
				this,
				-1m,
				dealer,
				null);
			return Amount < previousAmount;
		}

		SetAmount(previousAmount - 1);
		await ApplyLayerStatDelta(-1, dealer, null);
		if (Amount <= 0)
		{
			await PowerCmd.Remove(this);
		}

		return true;
	}

	private static async Task<FourWheeledCartPower> ApplyOneLayerDuringCombatEnd(
		Creature recipient,
		Creature dealer)
	{
		if (recipient.GetPower<FourWheeledCartPower>() is { } existing)
		{
			existing.SetAmount(existing.Amount + 1);
			await existing.ApplyLayerStatDelta(1, dealer, null);
			return existing;
		}

		var created = (FourWheeledCartPower)ModelDb
			.Power<FourWheeledCartPower>()
			.ToMutable();
		created.Applier = dealer;
		created.ApplyInternal(recipient, 1m);
		await created.ApplyLayerStatDelta(1, dealer, null);
		return created;
	}

	private static Creature ResolveTransferRecipient(Creature dealer) =>
		dealer.PetOwner?.Creature ?? dealer;

	private static async Task FlushAsync(
		PlayerChoiceContext choiceContext,
		Func<PendingTransfer, bool> predicate)
	{
		FourWheeledCartPower[] ready = PendingPowers
			.Where(power => power.PendingTransferList.Any(predicate))
			.ToArray();
		foreach (FourWheeledCartPower power in ready)
		{
			PendingTransfer[] transfers = power.PendingTransferList
				.Where(predicate)
				.ToArray();
			foreach (PendingTransfer transfer in transfers)
			{
				power.PendingTransferList.Remove(transfer);
				await power.TransferTo(choiceContext, transfer.Dealer);
			}

			if (power.PendingTransferList.Count == 0)
			{
				PendingPowers.Remove(power);
			}
		}
	}

	private static async Task FlushAfterCardAsync(Task playTask, CardModel card)
	{
		await playTask;
		await FlushAsync(
			new ThrowingPlayerChoiceContext(),
			transfer => ReferenceEquals(transfer.CardSource, card));
	}

	private static async Task FlushAfterMonsterMoveAsync(Task moveTask, Creature monster)
	{
		await moveTask;
		await FlushAsync(
			new ThrowingPlayerChoiceContext(),
			transfer => transfer.CardSource is null
				&& ReferenceEquals(transfer.Dealer, monster));
	}

	private static async Task<AttackCommand> FlushAfterStandaloneAttackAsync(
		Task<AttackCommand> attackTask,
		AttackCommand command,
		PlayerChoiceContext? choiceContext)
	{
		AttackCommand completedCommand = await attackTask;
		Creature? attacker = command.Attacker;
		if (attacker is null
			|| command.ModelSource is CardModel
			|| attacker.Monster?.IsPerformingMove == true)
		{
			return completedCommand;
		}

		await FlushAsync(
			choiceContext ?? new ThrowingPlayerChoiceContext(),
			transfer => transfer.CardSource is null
				&& ReferenceEquals(transfer.Dealer, attacker));
		return completedCommand;
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
	private static class CardResolutionPatch
	{
		private static void Postfix(CardModel __instance, ref Task __result)
		{
			__result = FlushAfterCardAsync(__result, __instance);
		}
	}

	[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
	private static class MonsterMoveResolutionPatch
	{
		private static void Postfix(MonsterModel __instance, ref Task __result)
		{
			__result = FlushAfterMonsterMoveAsync(__result, __instance.Creature);
		}
	}

	[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
	private static class StandaloneAttackResolutionPatch
	{
		private static void Postfix(
			AttackCommand __instance,
			PlayerChoiceContext? choiceContext,
			ref Task<AttackCommand> __result)
		{
			__result = FlushAfterStandaloneAttackAsync(
				__result,
				__instance,
				choiceContext);
		}
	}
}
