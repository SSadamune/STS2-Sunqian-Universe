using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "transparent_hole")]
public sealed class TransparentHole : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 7;
	public const int UpgradedDamage = 10;

	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TransparentHole.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public TransparentHole()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => ResolveEnergyXValue();

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		int xValue = ResolveEnergyXValue();
		if (xValue <= 0)
		{
			return;
		}

		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			this,
			choiceContext,
			xValue);

		int aliveEnemyCount = combatState.HittableEnemies.Count(creature => creature.IsAlive);
		if (aliveEnemyCount >= xValue)
		{
			return;
		}

		await AutoPlayFollowUp(choiceContext, combatState, xValue);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private async Task AutoPlayFollowUp(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		int xValue)
	{
		int followUpX = xValue - 1;
		if (followUpX < 0)
		{
			return;
		}

		CardModel followUp = GeneratedCombatCards.CreateInCombat<TransparentHole>(combatState, Owner, IsUpgraded);
		followUp.EnergyCost.CapturedXValue = followUpX;
		followUp.AddKeyword(CardKeyword.Exhaust);

		await CardCmd.AutoPlay(choiceContext, followUp, null, skipXCapture: true);
	}
}
