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

/// <summary>文关羽：每打出一张技能牌，获得受到敏捷加成的格挡。</summary>
[RegisterPower]
public sealed class GuanYuCivilVerPower : ModPowerTemplate
{
	private const string EffectiveBlockVarName = "EffectiveBlock";

	private const string DexterityMagnitudeVarName = "DexterityMagnitude";

	public const string NormalFormsVarName = "NormalForms";

	public const string UpgradedFormsVarName = "UpgradedForms";

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

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0
			|| !cardPlay.IsLastInSeries
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.Card.Type != CardType.Skill)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay: null);
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

		await GuanDiFormChoice.OfferRechoiceAsync(this, combatState);
	}
}
