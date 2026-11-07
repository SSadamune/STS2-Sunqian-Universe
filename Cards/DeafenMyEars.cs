#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "deafen_my_ears")]
public sealed class DeafenMyEars : ModCardTemplate
{
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Dragon];
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Multiplier", 4)];
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/DeafenMyEars.png");
	public DeafenMyEars() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
	protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		foreach (var card in Owner.PlayerCombatState!.Hand.Cards.ToArray())
		{
			if (SelfHarmSystem.HasSelfHarm(card)) continue;
			int cost = card.EnergyCost.CostsX ? Owner.PlayerCombatState.Energy
				: Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
			SelfHarmSystem.Grant(card, cost * DynamicVars["Multiplier"].IntValue);
		}
		return Task.CompletedTask;
	}
	protected override void OnUpgrade() => DynamicVars["Multiplier"].UpgradeValueBy(-1);
}
