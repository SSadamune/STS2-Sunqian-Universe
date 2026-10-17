using System.Collections.Generic;
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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GreatWeiChoirPower : ModPowerTemplate
{
	private sealed class Data
	{
		public int RoundNumber = -1;
		public CombatSide? Side;
		public int TriggerCount;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	protected override object InitInternalData() => new Data();

	public static async Task OnBurningExtinguishedAsync(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		decimal extinguishedAmount,
		CardModel? cardSource)
	{
		if (extinguishedAmount <= 0m)
		{
			return;
		}

		List<GreatWeiChoirPower> choirs = [];
		foreach (Creature playerCreature in combatState.PlayerCreatures)
		{
			if (playerCreature.GetPower<GreatWeiChoirPower>() is { } choir)
			{
				choirs.Add(choir);
			}
		}

		foreach (GreatWeiChoirPower choir in choirs)
		{
			await choir.TryTriggerAsync(
				choiceContext,
				combatState,
				extinguishedAmount,
				cardSource);
		}
	}

	private async Task TryTriggerAsync(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		decimal extinguishedAmount,
		CardModel? cardSource)
	{
		if (Owner.IsDead || Amount <= 0m)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (data.RoundNumber != combatState.RoundNumber
			|| data.Side != combatState.CurrentSide)
		{
			data.RoundNumber = combatState.RoundNumber;
			data.Side = combatState.CurrentSide;
			data.TriggerCount = 0;
		}

		if (data.TriggerCount >= Amount)
		{
			return;
		}

		data.TriggerCount++;
		Flash();
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner,
			extinguishedAmount,
			Owner,
			cardSource);
	}
}
