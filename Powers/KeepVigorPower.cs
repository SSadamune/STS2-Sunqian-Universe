using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
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
/// 活力保持：消耗活力后立刻获得等量活力。层数仅在攻击牌结算后减少。
/// </summary>
[RegisterPower]
public sealed class KeepVigorPower : ModPowerTemplate
{
	private static readonly Type? VigorInternalDataType =
		AccessTools.Inner(typeof(VigorPower), "Data");

	private static readonly MethodInfo? GetInternalDataMethod =
		VigorInternalDataType is null
			? null
			: AccessTools.Method(typeof(PowerModel), "GetInternalData", System.Type.EmptyTypes)
				?.MakeGenericMethod(VigorInternalDataType);

	private static readonly FieldInfo? CommandToModifyField =
		VigorInternalDataType is null
			? null
			: AccessTools.Field(VigorInternalDataType, "commandToModify");

	private static readonly FieldInfo? AmountWhenAttackStartedField =
		VigorInternalDataType is null
			? null
			: AccessTools.Field(VigorInternalDataType, "amountWhenAttackStarted");

	private bool _isRefunding;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/KeepVigorPower.png",
		BigIconPath: "res://images/powers/KeepVigorPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (_isRefunding
			|| Owner.IsDead
			|| Amount <= 0m
			|| power is not VigorPower vigor
			|| power.Owner != Owner
			|| amount >= 0m)
		{
			return;
		}

		_isRefunding = true;
		try
		{
			Flash();
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				-amount,
				Owner,
				cardSource);

			// 原版活力会把「本段攻击」绑在打出的那张牌上；消耗后层数为 0，其它牌预览自然归零。
			// 退回层数后若不解开绑定，ModifyDamageAdditive 会对非绑定牌返回 0，手牌打击便不再吃活力。
			ClearVigorAttackBinding(vigor);
		}
		finally
		{
			_isRefunding = false;
		}
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| cardPlay.Card.Type != CardType.Attack
			|| ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(cardPlay.Card))
		{
			return;
		}

		Flash();
		await PowerCmd.Decrement(this);
	}

	/// <summary>
	/// 清掉 <see cref="VigorPower"/> 在 <c>BeforeAttack</c> 写入的攻击绑定，
	/// 让退回后的活力能再次作用于任意攻击牌。
	/// </summary>
	private static void ClearVigorAttackBinding(VigorPower vigor)
	{
		if (GetInternalDataMethod is null
			|| CommandToModifyField is null
			|| AmountWhenAttackStartedField is null)
		{
			return;
		}

		object? data = GetInternalDataMethod.Invoke(vigor, null);
		if (data is null)
		{
			return;
		}

		CommandToModifyField.SetValue(data, null);
		AmountWhenAttackStartedField.SetValue(data, 0);
	}
}
