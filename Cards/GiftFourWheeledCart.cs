using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

// Temporarily retired from the card pool. Restore this attribute to revive it.
// [RegisterCard(typeof(SunqianCardPool), StableEntryStem = "gift_four_wheeled_cart")]
public sealed class GiftFourWheeledCart : ModCardTemplate
{
	public const int StrengthAmount = 2;
	public const int DexterityAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(StrengthAmount),
		new PowerVar<DexterityPower>(DexterityAmount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<FourWheeledCartPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GiftFourWheeledCart.png");

	public GiftFourWheeledCart()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<Creature> candidates = CombatState?.Creatures
			.Where(static creature =>
				creature.IsAlive
				&& creature.CanReceivePowers
				&& !creature.IsPet
				&& !creature.HasPower<MinionPower>())
			.ToList() ?? [];
		if (candidates.Count == 0)
		{
			return;
		}

		Creature target = Owner.RunState.Rng.CombatTargets.NextItem(candidates)
			?? throw new InvalidOperationException("Expected at least one Four-Wheeled Cart target.");
		await PowerCmd.Apply<FourWheeledCartPower>(
			choiceContext,
			target,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
