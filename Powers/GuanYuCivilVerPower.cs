using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>文关羽：牌结算后按实际耗能获得仅受敏捷影响的格挡。</summary>
[RegisterPower]
public sealed class GuanYuCivilVerPower : ModPowerTemplate
{
	private const string EffectiveBlockVarName = "EffectiveBlock";

	private const string DexterityMagnitudeVarName = "DexterityMagnitude";

	public const string NormalFormsVarName = "NormalForms";

	public const string UpgradedFormsVarName = "UpgradedForms";

	private sealed class Data
	{
		public Dictionary<CardModel, int> PendingEnergySpent { get; } = [];
	}

	private sealed class OwnerDexterityMagnitudeVar()
		: DynamicVar(DexterityMagnitudeVarName, 0)
	{
		private decimal CurrentValue
		{
			get
			{
				int dexterity = _owner is GuanYuCivilVerPower power
					? power.CurrentDexterity
					: 0;
				return Math.Abs(dexterity);
			}
		}

		protected override decimal GetBaseValueForIConvertible() => CurrentValue;

		public override string ToString() =>
			((int)CurrentValue).ToString(CultureInfo.InvariantCulture);
	}

	private sealed class OwnerEffectiveBlockVar()
		: DynamicVar(EffectiveBlockVarName, 0)
	{
		private decimal CurrentValue =>
			_owner is GuanYuCivilVerPower power
				? Math.Max(0, power.Amount + power.CurrentDexterity)
				: 0;

		protected override decimal GetBaseValueForIConvertible() => CurrentValue;

		public override string ToString() =>
			((int)CurrentValue).ToString(CultureInfo.InvariantCulture);
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override string SmartDescriptionLocKey =>
		CurrentDexterity switch
		{
			> 0 => base.SmartDescriptionLocKey + "Positive",
			< 0 => base.SmartDescriptionLocKey + "Negative",
			_ => base.SmartDescriptionLocKey,
		};

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new OwnerEffectiveBlockVar(),
		new OwnerDexterityMagnitudeVar(),
		new DynamicVar(NormalFormsVarName, 0),
		new DynamicVar(UpgradedFormsVarName, 0),
	];

	private int CurrentDexterity =>
		IsMutable && Owner is { } owner
			? owner.GetPower<DexterityPower>()?.Amount ?? 0
			: 0;

	public int NormalFormCount => DynamicVars[NormalFormsVarName].IntValue;

	public int UpgradedFormCount => DynamicVars[UpgradedFormsVarName].IntValue;

	public int FormCount => NormalFormCount + UpgradedFormCount;

	public bool FormUpgraded => UpgradedFormCount > 0;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanYuCivilVerPower.png",
		BigIconPath: "res://images/powers/GuanYuCivilVerPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public void SetFormCounts(int normalForms, int upgradedForms)
	{
		DynamicVars[NormalFormsVarName].BaseValue = normalForms;
		DynamicVars[UpgradedFormsVarName].BaseValue = upgradedForms;
	}

	protected override object InitInternalData() => new Data();

	public override async Task AfterCardPlayedLate(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (!cardPlay.IsLastInSeries || cardPlay.Card.Owner.Creature != Owner)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.PendingEnergySpent.Remove(cardPlay.Card, out int energySpent);
		if (Owner.IsDead || Amount <= 0 || energySpent <= 0)
		{
			return;
		}

		decimal blockPerEnergy = Math.Max(0, Amount + CurrentDexterity);
		if (blockPerEnergy <= 0m)
		{
			return;
		}

		Flash();
		await GainDexterityOnlyBlockAsync(
			Owner,
			blockPerEnergy * energySpent,
			cardPlay);
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

	private static async Task GainDexterityOnlyBlockAsync(
		Creature creature,
		decimal blockAmount,
		CardPlay cardPlay)
	{
		if (CombatManager.Instance.IsOverOrEnding
			|| creature.IsDead
			|| creature.CombatState is not { } combatState)
		{
			return;
		}

		ValueProp props = ValueProp.Unpowered;
		await Hook.BeforeBlockGained(
			combatState,
			creature,
			blockAmount,
			props,
			cardPlay.Card);
		await Hook.AfterModifyingBlockAmount(
			combatState,
			blockAmount,
			cardPlay.Card,
			cardPlay,
			Array.Empty<AbstractModel>());

		SfxCmd.Play("event:/sfx/block_gain");
		VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_block");
		creature.GainBlockInternal(blockAmount);
		CombatManager.Instance.History.BlockGained(
			combatState,
			creature,
			(int)blockAmount,
			props,
			cardPlay);
		await Cmd.CustomScaledWait(0.1f, 0.25f);
		await Hook.AfterBlockGained(
			combatState,
			creature,
			blockAmount,
			props,
			cardPlay.Card);
	}
}
