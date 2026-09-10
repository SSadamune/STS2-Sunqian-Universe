using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 缺草送马：打出抽牌。蓄能累计消耗 2 能量后改为抽 2（升级 3）张；不够则仍为 0。打出后解除。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "send_horse_without_hay")]
public sealed class SendHorseWithoutHay : ChargeCardTemplate
{
	public const int CanonicalDraw = 0;
	public const int EnergyThreshold = 2;
	public const int UnupgradedDraw = 2;
	public const int UpgradedDraw = 3;

	private int _spentEnergy;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(CanonicalDraw),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnTurnEndInHand: null,
		OnPowerAmountChanged: null,
		Clear: ResetCharge,
		OnCardPlayed: AccumulateSpentEnergy);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SendHorseWithoutHay.png");

	protected override bool ShouldGlowGoldInternal => IsCharged;

	public SendHorseWithoutHay()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	private int PrintedDraw => IsUpgraded ? UpgradedDraw : UnupgradedDraw;

	private bool IsCharged => _spentEnergy >= EnergyThreshold;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
	}

	protected override void OnUpgrade()
	{
		RefreshDrawnCards();
	}

	private Task AccumulateSpentEnergy(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner || cardPlay.PlayIndex != 0)
		{
			return Task.CompletedTask;
		}

		int energySpent = cardPlay.Resources.EnergySpent;
		if (energySpent <= 0)
		{
			return Task.CompletedTask;
		}

		_spentEnergy += energySpent;
		RefreshDrawnCards();
		return Task.CompletedTask;
	}

	private void RefreshDrawnCards()
	{
		DynamicVars.Cards.BaseValue = IsCharged ? PrintedDraw : CanonicalDraw;
	}

	private void ResetCharge()
	{
		_spentEnergy = 0;
		RefreshDrawnCards();
	}
}
