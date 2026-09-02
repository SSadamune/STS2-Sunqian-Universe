using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "benevolence_righteousness_dual_swords")]
public sealed class BenevolenceRighteousnessDualSwords : ModCardTemplate
{
	public const decimal ExtraPlaysPerCard = 1m;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BenevolenceRighteousnessDualSwords.png");

	public BenevolenceRighteousnessDualSwords()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.DualSwordsEvent);
		await PowerCmd.Apply<BenevolenceRighteousnessDualSwordsPower>(
			choiceContext,
			Owner.Creature,
			ExtraPlaysPerCard,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 1, costsX: false));
		InvokeEnergyCostChanged();
	}
}
