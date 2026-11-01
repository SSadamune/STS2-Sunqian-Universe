using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "i_have_a_famous_quote_too")]
public sealed class IHaveAFamousQuoteToo : ModCardTemplate
{
	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// Temporary portrait until dedicated card art is available.
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/DrinkingTableCulture.png");

	public IHaveAFamousQuoteToo()
		: base(1, CardType.Skill, CardRarity.Uncommon, SquTargetTypes.LowerHpAlly)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		var target = cardPlay.Target;
		var self = Owner.Creature;
		// Recheck at resolution in case another multiplayer action changed HP.
		if (!target.IsAlive || !target.IsPlayer || target == self
			|| target.Side != self.Side || target.CurrentHp >= self.CurrentHp)
		{
			return;
		}

		int sharedHp = (int)(((long)self.CurrentHp + target.CurrentHp) / 2);
		await CreatureCmd.SetCurrentHp(self, sharedHp);
		await CreatureCmd.SetCurrentHp(target, sharedHp);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
