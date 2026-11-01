#nullable enable
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using Squ.Combat;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>Resolves the first-play life payment once, even for repeated or auto-played cards.</summary>
public abstract class SelfHarmCardTemplate : ModCardTemplate, ISelfHarmCard
{
	public decimal SelfHarmAmount => DynamicVars["SelfHarm"].BaseValue;
	protected SelfHarmCardTemplate(int cost, CardType type, CardRarity rarity, TargetType target)
		: base(cost, type, rarity, target) { }
	protected sealed override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		int paid = await SelfHarmSystem.ResolveFirstPlay(context, this, play);
		if (paid > 0) await AfterSelfHarmPaid(context, paid);
		if (Owner.Creature.IsAlive) await PlayAfterSelfHarm(context, play);
	}
	protected virtual Task AfterSelfHarmPaid(PlayerChoiceContext context, int amount) => Task.CompletedTask;
	protected sealed override void AddExtraArgsToDescription(LocString description)
	{
		LocString clause = new("card_keywords", SelfHarmSystem.HasTriggered(this)
			? "SUNQIAN_UNIVERSE_SELF_HARM.spentCardText" : "SUNQIAN_UNIVERSE_SELF_HARM.cardText");
		clause.AddObj("SelfHarm", DynamicVars["SelfHarm"]);
		SquKeywords.AddNestedLoc(description, "SelfHarmText", clause);
		AddExtraSelfHarmArgs(description);
	}
	protected virtual void AddExtraSelfHarmArgs(LocString description) { }
	protected abstract Task PlayAfterSelfHarm(PlayerChoiceContext context, CardPlay play);
}
