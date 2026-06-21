using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JingranBuxu : ModCardTemplate
{
	private bool _isApplyingOwnDebuff;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(4m, ValueProp.Move),
		new PowerVar<VulnerablePower>(1),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<ArtifactPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/JingranBuxu.png");

	public JingranBuxu()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);

		_isApplyingOwnDebuff = true;
		try
		{
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

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (!CanReturnToHand())
		{
			return;
		}

		if (dealer != Owner.Creature || cardSource == this || !result.WasFullyBlocked)
		{
			return;
		}

		await CardPileCmd.Add(this, PileType.Hand);
	}

	public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
		if (!CanReturnToHand() || _isApplyingOwnDebuff)
		{
			return;
		}

		if (power.Applier != Owner.Creature
			|| power.Type != PowerType.Debuff
			|| power.Owner != null)
		{
			return;
		}

		await CardPileCmd.Add(this, PileType.Hand);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}

	private bool CanReturnToHand() =>
		Pile?.Type != PileType.Hand && !HasPlayedThisTurn();

	private bool HasPlayedThisTurn() =>
		CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
			entry.HappenedThisTurn(CombatState)
			&& entry.CardPlay.Card == this);
}
