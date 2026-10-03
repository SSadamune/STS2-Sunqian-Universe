using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
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

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class NeverHadThesePower : ModPowerTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(0),
	];

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/NeverHadThesePower.png",
		BigIconPath: "res://images/powers/NeverHadThesePowerBig.png");

	protected override IEnumerable<string> RegisteredKeywordIds => [SquKeywords.FamilyId];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
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
			AddDexterityStack();
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| card.Owner?.Creature != Owner
			|| !card.HasFamily()
			|| Owner.Player is not { } player)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(Amount, player);
		await PowerCmd.Apply<DexterityPower>(
			choiceContext,
			Owner,
			DynamicVars[nameof(DexterityPower)].BaseValue,
			Owner,
			card);
	}

	private void AddDexterityStack()
	{
		DynamicVars[nameof(DexterityPower)].BaseValue += NeverHadThese.DexterityGain;
	}
}
