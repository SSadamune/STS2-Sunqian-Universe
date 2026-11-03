#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>Display-only option, never added to a deck or played as a card.</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "brother_attack_choice")]
public sealed class BrotherAttackChoice : ModCardTemplate
{
	public string CompanionId { get; private set; } = CompanionSummons.OstyId;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CompanionDamageVar(CompanionSummons.SecondBrotherId, 0, card => ((BrotherAttackChoice)card).CompanionId)];
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: CompanionSummons.GetPortraitPath(CompanionId));
	public override string Title => CompanionSummons.GetName(CompanionId).GetFormattedText();
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[new HoverTip(MegaCrit.Sts2.Core.Models.ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.DieForYouPower>(),
			new LocString("powers", "SUNQIAN_UNIVERSE_POWER_COMPANION_DIE_FOR_YOU.description").GetFormattedText(), false)];
	public BrotherAttackChoice() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }
	internal void Configure(string id, decimal amount)
	{
		AssertMutable();
		CompanionId = id;
		DynamicVars["CompanionDamage"].BaseValue = amount;
	}
	protected override void AddExtraArgsToDescription(LocString description) =>
		description.Add("SummonName", CompanionSummons.GetName(CompanionId));
	protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
