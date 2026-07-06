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

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "benevolence_righteousness_dual_swords")]
public sealed class BenevolenceRighteousnessDualSwords : ModCardTemplate
{
	private const decimal BaseExtraPlays = 1m;
	private const decimal UpgradedExtraPlays = 2m;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BenevolenceRighteousnessDualSwords.png");

	public BenevolenceRighteousnessDualSwords()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal extraPlays = IsUpgraded ? UpgradedExtraPlays : BaseExtraPlays;
		await PowerCmd.Apply<BenevolenceRighteousnessDualSwordsPower>(
			choiceContext,
			Owner.Creature,
			extraPlays,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
	}
}
