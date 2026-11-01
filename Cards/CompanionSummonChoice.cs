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
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "companion_summon_choice")]
public sealed class CompanionSummonChoice : ModCardTemplate
{
	public string CompanionId { get; private set; } = CompanionSummons.OstyId;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(0)];
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: CompanionSummons.GetPortraitPath(CompanionId));
	public override string Title => CompanionSummons.GetName(CompanionId).GetFormattedText();
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[CompanionSummonKeywords.NamedHoverTip(CompanionId, DynamicVars.Summon)];
	public CompanionSummonChoice() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }
	internal void Configure(string id, decimal amount)
	{
		AssertMutable();
		CompanionId = id;
		DynamicVars.Summon.BaseValue = amount;
	}
	protected override void AddExtraArgsToDescription(LocString description) =>
		description.Add("SummonName", CompanionSummons.GetName(CompanionId));
	protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
