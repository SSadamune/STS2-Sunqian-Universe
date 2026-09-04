using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(WeakAmount),
		new PowerVar<BurningPower>(BaseBurning),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
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
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(WeakPower)].BaseValue,
			Owner.Creature,
			this);

		decimal burningStacks = DynamicVars[nameof(BurningPower)].BaseValue;
		List<BurningPower> appliedBurning = [];
		foreach (Creature enemy in combatState.HittableEnemies)
		{
			if (!enemy.IsAlive)
			{
				continue;
			}

			BurningPower? burning = await PowerCmd.Apply<BurningPower>(
				choiceContext,
				enemy,
				burningStacks,
				Owner.Creature,
				this);
			if (burning != null)
			{
				appliedBurning.Add(burning);
			}
		}

		if (PlayedAttackThisTurn(combatState))
		{
			SquSfx.Play(SquSfx.BlockFirefightingDoNotDisturbEvent);
			return;
		}

		SquSfx.Play(SquSfx.BlockFirefightingSageEvent);
		foreach (BurningPower burning in appliedBurning)
		{
			burning.ReduceClearChanceTo(0f);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}

	private bool PlayedAttackThisTurn(ICombatState combatState) =>
		CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
			entry.HappenedThisTurn(combatState)
			&& entry.CardPlay.Card.Owner == Owner
			&& entry.CardPlay.Card.Type == CardType.Attack);
}
