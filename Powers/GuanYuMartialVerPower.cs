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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 武关羽：记录一张牌实际支付的能量，并在整张牌结算后按比例给予活力。
/// </summary>
[RegisterPower]
public sealed class GuanYuMartialVerPower : ModPowerTemplate
{
	public const string NormalFormsVarName = "NormalForms";

	public const string UpgradedFormsVarName = "UpgradedForms";

	private sealed class Data
	{
		public Dictionary<CardModel, int> PendingEnergySpent { get; } = [];
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(NormalFormsVarName, 0),
		new DynamicVar(UpgradedFormsVarName, 0),
	];

	public int NormalFormCount => DynamicVars[NormalFormsVarName].IntValue;

	public int UpgradedFormCount => DynamicVars[UpgradedFormsVarName].IntValue;

	public int FormCount => NormalFormCount + UpgradedFormCount;

	public bool FormUpgraded => UpgradedFormCount > 0;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanYuMartialVerPower.png",
		BigIconPath: "res://images/powers/GuanYuMartialVerPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.ForEnergy(this),
	];

	public void SetFormCounts(int normalForms, int upgradedForms)
	{
		DynamicVars[NormalFormsVarName].BaseValue = normalForms;
		DynamicVars[UpgradedFormsVarName].BaseValue = upgradedForms;
	}

	protected override object InitInternalData() => new Data();

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsLastInSeries || cardPlay.Card.Owner.Creature != Owner)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.PendingEnergySpent.Remove(cardPlay.Card, out int energySpent);
		if (!Owner.IsDead && Amount > 0 && energySpent > 0)
		{
			Flash();
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				Amount * energySpent,
				Owner,
				cardPlay.Card);
		}
	}

	public override Task AfterEnergySpent(CardModel card, int energySpent)
	{
		TrackEnergySpent(card, energySpent);
		return Task.CompletedTask;
	}

	public void TrackEnergySpent(CardModel card, int energySpent)
	{
		if (!Owner.IsDead
			&& Amount > 0
			&& energySpent > 0
			&& card.Owner?.Creature == Owner)
		{
			GetInternalData<Data>().PendingEnergySpent[card] = energySpent;
		}
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

		GetInternalData<Data>().PendingEnergySpent.Clear();
		await GuanDiFormChoice.OfferRechoiceAsync(this, combatState);
	}
}
