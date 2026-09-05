using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "block_firefighting")]
public sealed class BlockFirefighting : ModCardTemplate
{
	public const decimal WeakAmount = 1m;
	public const decimal BaseBurning = 6m;
	public const decimal UpgradedBurning = 8m;
	public const decimal BaseUnextinguished = 2m;
	public const decimal UpgradedUnextinguished = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BaseBurning),
		new PowerVar<WeakPower>(WeakAmount),
		new PowerVar<UnextinguishedPower>(BaseUnextinguished),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<UnextinguishedPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BlockFirefighting.png");

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			ICombatState? combatState = CombatState;
			if (combatState == null || combatState.CurrentSide != CombatSide.Player)
			{
				return false;
			}

			return !PlayedAttackThisTurn(combatState);
		}
	}

	public BlockFirefighting()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(BurningPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(WeakPower)].BaseValue,
			Owner.Creature,
			this);

		if (PlayedAttackThisTurn(combatState))
		{
			SquSfx.Play(SquSfx.BlockFirefightingDoNotDisturbEvent);
			return;
		}

		SquSfx.Play(SquSfx.BlockFirefightingSageEvent);
		await PowerCmd.Apply<UnextinguishedPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(UnextinguishedPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
		DynamicVars[nameof(UnextinguishedPower)].UpgradeValueBy(UpgradedUnextinguished - BaseUnextinguished);
	}

	private bool PlayedAttackThisTurn(ICombatState combatState) =>
		CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
			entry.HappenedThisTurn(combatState)
			&& entry.CardPlay.Card.Owner == Owner
			&& entry.CardPlay.Card.Type == CardType.Attack);
}
