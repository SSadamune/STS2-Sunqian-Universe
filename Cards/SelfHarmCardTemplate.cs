#nullable enable
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Combat;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>Resolves and removes the current self-harm keyword before the card effects.</summary>
public abstract class SelfHarmCardTemplate : ModCardTemplate, ISelfHarmCard, IRowKeywordsCard
{
	public decimal SelfHarmAmount => DynamicVars["SelfHarm"].BaseValue;
	protected SelfHarmCardTemplate(int cost, CardType type, CardRarity rarity, TargetType target)
		: base(cost, type, rarity, target) { }
	protected sealed override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		int paid = await SelfHarmSystem.ResolvePayment(context, this, play);
		if (paid > 0) await AfterSelfHarmPaid(context, paid);
		if (Owner.Creature.IsAlive) await PlayAfterSelfHarm(context, play);
	}
	protected virtual Task AfterSelfHarmPaid(PlayerChoiceContext context, int amount) => Task.CompletedTask;
	protected sealed override void AddExtraArgsToDescription(LocString description)
	{
		if (!SelfHarmSystem.HasSelfHarm(this))
			description.Add("SelfHarmText", "");
		else
		{
			LocString clause = new("card_keywords", "SUNQIAN_UNIVERSE_SELF_HARM.cardText");
			clause.AddObj("SelfHarm", SelfHarmSystem.GetAmountVar(this));
			SquKeywords.AddNestedLoc(description, "SelfHarmText", clause);
		}
		AddExtraSelfHarmArgs(description);
	}
	protected virtual void AddExtraSelfHarmArgs(LocString description) { }
	protected abstract Task PlayAfterSelfHarm(PlayerChoiceContext context, CardPlay play);
}
