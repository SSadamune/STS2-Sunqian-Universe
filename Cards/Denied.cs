using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 被动回手：当本牌位于抽牌堆/弃牌堆/消耗堆时，若玩家通过其他来源施加的 debuff
/// 被人工制品抵消，则将此牌移入手牌（参考 Regent「Make It So」的 CardPileCmd.Add 模式）。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "denied")]
public sealed class Denied : ModCardTemplate
{
	/// <summary>打出本牌施加 debuff 时为 true，避免触发自身的被动回手。</summary>
	private bool _isApplyingOwnDebuff;

	/// <summary>正在观察的一次 debuff 施加：记录施加前层数，供事后对比结果。</summary>
	private PendingDebuffApplication? _pendingDebuffApplication;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(1),
		new PowerVar<VulnerablePower>(1),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<ArtifactPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Denied.png");

	public Denied()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		_isApplyingOwnDebuff = true;
		try
		{
			await PowerCmd.Apply<WeakPower>(
				choiceContext,
				cardPlay.Target,
				DynamicVars[nameof(WeakPower)].BaseValue,
				Owner.Creature,
				this);

			await PowerCmd.Apply<VulnerablePower>(
				choiceContext,
				cardPlay.Target,
				DynamicVars[nameof(VulnerablePower)].BaseValue,
				Owner.Creature,
				this);
		}
		finally
		{
			_isApplyingOwnDebuff = false;
		}
	}

	/// <summary>
	/// 全局 hook：任意 power 即将施加到目标前都会调用（含抽牌堆/弃牌堆/消耗堆中的本牌实例）。
	/// 先结算上一次 pending，再为符合条件的 debuff 施加记录快照。
	/// </summary>
	public override async Task BeforePowerAmountChanged(
		PowerModel power,
		decimal amount,
		Creature target,
		Creature? applier,
		CardModel? cardSource)
	{
		await TryReturnToHandIfPendingDebuffFailedAsync();

		if (!ShouldTrackDebuffApplication(applier, amount, power, cardSource))
		{
			return;
		}

		int artifactBefore = target.GetPower<ArtifactPower>()?.Amount ?? 0;
		if (artifactBefore <= 0)
		{
			return;
		}

		_pendingDebuffApplication = new PendingDebuffApplication(
			target,
			power.Id,
			GetPowerAmount(target, power.Id),
			artifactBefore);
	}

	/// <summary>
	/// 返回 true 以加入 power 修改链，从而收到 <see cref="AfterModifyingPowerAmountReceived"/>。
	/// 不修改 amount，仅作观察者。
	/// </summary>
	public override bool TryModifyPowerAmountReceived(
		PowerModel canonicalPower,
		Creature target,
		decimal amount,
		Creature? applier,
		out decimal modifiedAmount)
	{
		modifiedAmount = amount;

		if (_pendingDebuffApplication is not PendingDebuffApplication pending)
		{
			return false;
		}

		return pending.Target == target && pending.PowerId == canonicalPower.Id;
	}

	/// <summary>debuff 结算后立即尝试回手（人工制品层数可能尚未扣除，见 TryReturn）。</summary>
	public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
		if (_pendingDebuffApplication is null || _pendingDebuffApplication.Value.PowerId != power.Id)
		{
			return;
		}

		await TryReturnToHandIfPendingDebuffFailedAsync();
	}

	/// <summary>
	/// 卡牌打出流程结束后再检查一次（与 Make It So 相同 hook）。
	/// 此时人工制品通常已扣层，是 pending 的最终兜底结算点。
	/// </summary>
	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await TryReturnToHandIfPendingDebuffFailedAsync();
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(WeakPower)].UpgradeValueBy(1m);
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);
	}

	private bool ShouldTrackDebuffApplication(
		Creature? applier,
		decimal amount,
		PowerModel power,
		CardModel? cardSource) =>
		CanReturnToHand()
		&& !_isApplyingOwnDebuff
		&& cardSource != this
		&& applier == Owner.Creature
		&& amount > 0m
		&& power.IsVisible
		&& power.GetTypeForAmount(amount) == PowerType.Debuff;

	/// <summary>
	/// 对比 pending 快照与当前状态：debuff 层数未增加且人工制品被消耗 → 回手。
	/// 若 debuff 未增加但人工制品尚未扣层，保留 pending 等待后续 hook。
	/// </summary>
	private async Task TryReturnToHandIfPendingDebuffFailedAsync()
	{
		if (_pendingDebuffApplication is not PendingDebuffApplication pending || !CanReturnToHand())
		{
			return;
		}

		int amountAfter = GetPowerAmount(pending.Target, pending.PowerId);
		if (amountAfter > pending.AmountBefore)
		{
			_pendingDebuffApplication = null;
			return;
		}

		int artifactAfter = pending.Target.GetPower<ArtifactPower>()?.Amount ?? 0;
		if (artifactAfter >= pending.ArtifactBefore)
		{
			// ArtifactPower 的 AfterModifying 可能在本回调之后执行，此时层数尚未变化。
			return;
		}

		_pendingDebuffApplication = null;
		await CardPileCmd.Add(this, PileType.Hand);
	}

	private static bool CanReturnToHand(PileType? pileType) =>
		pileType is PileType.Draw or PileType.Discard or PileType.Exhaust;

	private bool CanReturnToHand() => CanReturnToHand(Pile?.Type);

	private static int GetPowerAmount(Creature target, ModelId powerId) =>
		target.GetPower(powerId)?.Amount ?? 0;

	/// <summary>某次 debuff 施加前的目标状态快照。</summary>
	private readonly record struct PendingDebuffApplication(
		Creature Target,
		ModelId PowerId,
		int AmountBefore,
		int ArtifactBefore);
}
