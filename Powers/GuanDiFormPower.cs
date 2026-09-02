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
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanDiFormPower : ModPowerTemplate
{
	/// <summary>叠加后的攻击牌临时力量总量；敏捷总量由原版 <see cref="PowerModel.Amount"/> 追踪。</summary>
	private decimal _strengthAmount;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(1),
		new PowerVar<StrengthPower>(2),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this && amount > 0m)
		{
			// 首次 Apply 与叠层都会走这里；勿在 AfterApplied 再记一次，否则会双计力量。
			_strengthAmount += GetStrengthContribution(cardSource);
			SyncDynamicVarsFromTotals();
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead || cardPlay.Card.Owner != Owner.Player)
		{
			return;
		}

		switch (cardPlay.Card.Type)
		{
			case CardType.Skill:
				Flash();
				await PowerCmd.Apply<TempDexFromGuanDiFormPower>(
					choiceContext, Owner, Amount, Owner, null);
				break;

			case CardType.Attack:
				Flash();
				await PowerCmd.Apply<TempStrFromGuanDiFormPower>(
					choiceContext, Owner, _strengthAmount, Owner, null);
				break;
		}
	}

	private static decimal GetStrengthContribution(CardModel? cardSource) =>
		cardSource is GuanDiForm card
			? card.DynamicVars[nameof(StrengthPower)].BaseValue
			: 0m;

	private void SyncDynamicVarsFromTotals()
	{
		DynamicVars[nameof(DexterityPower)].BaseValue = Amount;
		DynamicVars[nameof(StrengthPower)].BaseValue = _strengthAmount;
	}
}

[RegisterPower]
public sealed class TempDexFromGuanDiFormPower : TempDexPower<GuanDiFormPower> { }

[RegisterPower]
public sealed class TempStrFromGuanDiFormPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
	private static readonly PowerModel SetupStrikePowerTemplate = ModelDb.Power<SetupStrikePower>();

	public override AbstractModel OriginModel => ModelDb.Card<GuanDiForm>();

	public PowerAssetProfile AssetProfile => new(
		IconPath: SetupStrikePowerTemplate.PackedIconPath,
		BigIconPath: SetupStrikePowerTemplate.ResolvedBigIconPath);

	public string? CustomIconPath => AssetProfile.IconPath;

	public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
