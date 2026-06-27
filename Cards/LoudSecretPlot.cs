using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "loud_secret_plot")]
public sealed class LoudSecretPlot : ModCardTemplate
{
	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Ethereal,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LoudSecretPlot.png");

	public LoudSecretPlot()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<LoudSecretPlotPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 1, costsX: false));
		InvokeEnergyCostChanged();
	}
}
