using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using Squ.Audio;
using Squ.Character;
using Squ.RunData;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "self_decapitation_ascension")]
public sealed class SelfDecapitationAscension : ModCardTemplate
{
	public const int IntangibleAmount = 1;

	protected override bool HasEnergyCostX => true;

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IntangiblePower>(IntangibleAmount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>(),
		HoverTipFactory.ForEnergy(this),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SelfDecapitationAscension.png");

	public SelfDecapitationAscension()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.SelfDecapitationEvent);

		ICombatState combatState = CombatState
			?? throw new System.InvalidOperationException(
				"Self Decapitation Ascension requires an active combat.");
		int amount = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
		SelfDecapitationReviveData.Schedule(Owner, Owner.Creature.CurrentHp);
		await CreatureCmd.Kill(Owner.Creature, force: true);

		foreach (Player ally in combatState.Players.Where(player =>
			player != Owner && !player.Creature.IsDead))
		{
			await PowerCmd.Apply<IntangiblePower>(
				choiceContext,
				ally.Creature,
				DynamicVars[nameof(IntangiblePower)].BaseValue,
				Owner.Creature,
				this);

			if (amount > 0)
			{
				await PlayerCmd.GainEnergy(amount, ally);
				await CardPileCmd.Draw(choiceContext, amount, ally);
			}
		}
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("HasRevivalItem", HasRevivalItem);
	}

	private bool HasRevivalItem => IsMutable
		&& Owner is { } owner
		&& (owner.Potions.Any(potion => potion is FairyInABottle)
			|| owner.Relics.Any(relic => relic is LizardTail));
}
