using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "link_to_heaven")]
public sealed class LinkToHeaven : ModCardTemplate
{
	private const decimal ScryStacksPerPlay = 3m;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LinkToHeaven.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.ForEnergy(this),
	];

	public LinkToHeaven()
		: base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<LinkToHeavenPower>(
			choiceContext,
			Owner.Creature,
			ScryStacksPerPlay,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 1, costsX: false));
		InvokeEnergyCostChanged();
	}
}
