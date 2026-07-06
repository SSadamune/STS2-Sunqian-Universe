using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "all_in")]
public sealed class AllIn : ModCardTemplate
{
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/AllIn.png");

	public AllIn()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<AllInPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 0, costsX: false));
		InvokeEnergyCostChanged();
	}
}
