#nullable enable
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using STS2RitsuLib.Models.Capabilities;

namespace Squ.Combat;

/// <summary>Shows the Vigor-scaled follow-up damage granted to any basic Strike by Chicken Foot Cheese.</summary>
public sealed class ChickenFootCheeseStrikeCapability : CardCapability, ICardDescriptionContributor
{
	public const string FollowUpDamageVarName = "ChickenFootCheeseFollowUpDamage";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScriptFollowUpDamageVar(),
	];

	public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
	{
		if (context.PileType is not (PileType.Hand or PileType.Play)
			|| !ScriptChickenFootCheesePower.ShouldDisplayRedirectedBasicStrike(context.Card))
		{
			yield break;
		}

		yield return new CardDescriptionFragment(
			new LocString("cards", "SUNQIAN_UNIVERSE_CHICKEN_FOOT_CHEESE_STRIKE.descriptionFragment"),
			CardDescriptionFragmentPlacement.AfterBase);
	}

	private sealed class ScriptFollowUpDamageVar()
		: DamageVar(FollowUpDamageVarName, ScriptChickenFootCheesePower.BaseFollowUpDamage, ValueProp.Move)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
			bool runGlobalHooks)
		{
			if (!ScriptChickenFootCheesePower.ShouldDisplayRedirectedBasicStrike(card))
			{
				ResetToBase();
				return;
			}

			decimal followUp = card.Owner.Creature.GetPower<ScriptChickenFootCheesePower>()!.FollowUpDamage;
			// Capability BaseValue is saved and checksummed. Local UI previews must
			// only copy display fields, including the baseline for damage highlighting.
			var preview = new DamageVar(Name, followUp, Props);
			preview.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
			EnchantedValue = preview.EnchantedValue;
			PreviewValue = preview.PreviewValue;
		}
	}
}
