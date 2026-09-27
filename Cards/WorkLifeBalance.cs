using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 劳逸结合：多次获得格挡。蓄能：手牌中消耗活力后，每段格挡增加等量数值。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "work_life_balance")]
public sealed class WorkLifeBalance : ChargeCardTemplate
{
	public const int CanonicalBlock = 5;
	public const int UpgradedBlock = 4;
	public const int CanonicalRepeat = 2;
	public const int UpgradedRepeat = 3;

	private static readonly ValueProp BlockProps = ValueProp.Move;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ChargedBlockVar(
			CanonicalBlock,
			BlockProps,
			card => card.IsUpgraded ? UpgradedBlock : CanonicalBlock),
		new RepeatVar(CanonicalRepeat),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnRetained: null,
		OnPowerAmountChanged: GainBlockFromSpentVigor,
		Clear: ResetBlockUntilPlayed);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WorkLifeBalance.png");

	protected override bool ShouldGlowGoldInternal => DynamicVars.Block.BaseValue > PrintedBlock;

	public WorkLifeBalance()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.WorkLifeBalanceLetMeEnjoyEvent);
		for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - CanonicalBlock);
		DynamicVars.Repeat.UpgradeValueBy(UpgradedRepeat - CanonicalRepeat);
	}

	private Task GainBlockFromSpentVigor(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power is not VigorPower || power.Owner != Owner.Creature || amount >= 0m)
		{
			return Task.CompletedTask;
		}

		DynamicVars.Block.BaseValue += -amount;
		RefreshCardVisuals();
		return Task.CompletedTask;
	}

	private void ResetBlockUntilPlayed()
	{
		DynamicVars.Block.BaseValue = PrintedBlock;
		RefreshCardVisuals();
	}

	private decimal PrintedBlock => IsUpgraded ? UpgradedBlock : CanonicalBlock;
}
