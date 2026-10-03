using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "cute_pet")]
public sealed class CutePet : FamilyCardTemplate
{
	public const int SummonAmount = 4;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CutePet.png");

	public CutePet()
		: base(0)
	{
	}

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseCutePetExhaustEvent;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(SummonAmount),
		.. base.CanonicalVars,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		.. base.AdditionalHoverTips,
		HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
		SquKeywords.Family,
	];

	protected override Task ResolveAdditionalExhaust(
		PlayerChoiceContext choiceContext,
		bool causedByEthereal) =>
		OstyCmd.Summon(choiceContext, Owner, DynamicVars.Summon.BaseValue, this);
}
