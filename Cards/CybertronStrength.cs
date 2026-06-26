using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "cybertron_strength")]
public sealed class CybertronStrength : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CybertronStrength.png");

	public CybertronStrength()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = Owner.Creature.CombatState;
		if (combatState is null)
		{
			return;
		}

		int blockAmount = combatState.HittableEnemies
			.Where(creature => creature.IsAlive)
			.Sum(creature => creature.GetPowerAmount<BurningPower>());
		if (blockAmount <= 0)
		{
			return;
		}

		await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 0, costsX: false));
		InvokeEnergyCostChanged();
	}
}
