using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "burning_linked_camps_script")]
public sealed class BurningLinkedCampsScript : ScriptCardTemplate<BurningLinkedCampsScriptRecord>
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<Burn>(),
		SquCardHoverTipFactory.FromCard<ShangfangguSigh>(this, IsUpgraded),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BurningLinkedCampsScript.png");

	public BurningLinkedCampsScript()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.EmperorKnowsNoWarEvent);
		var combatState = Owner.Creature.CombatState
			?? throw new System.InvalidOperationException("BurningLinkedCampsScript requires an active combat.");
		await GeneratedCombatCards.AddToHandInCombat<Burn>(
			combatState,
			Owner,
			upgraded: false,
			Owner);
		await GeneratedCombatCards.AddToDrawPileInCombat<ShangfangguSigh>(
			combatState,
			Owner,
			1,
			IsUpgraded,
			Owner);

		await UnlockScriptAsync(choiceContext);
	}
}
