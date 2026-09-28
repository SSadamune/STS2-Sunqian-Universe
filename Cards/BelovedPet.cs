using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "beloved_pet")]
public sealed class BelovedPet : FamilyCardTemplate
{
	public const int SummonAmount = 6;

	public BelovedPet()
		: base(0)
	{
	}

	protected override int BlockAmount => 0;

	protected override int DrawCards => 0;

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

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return OstyCmd.Summon(choiceContext, Owner, DynamicVars.Summon.BaseValue, this);
	}
}
