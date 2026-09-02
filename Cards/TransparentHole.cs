using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
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

	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TransparentHole.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public TransparentHole()
		: base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => ResolveTargetCount();

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.TenThousandTransparentHolesEvent);

		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		int targetCount = ResolveTargetCount();
		if (targetCount <= 0)
		{
			return;
		}

		SquVigorSnapshot.AttackSequence vigorSequence = SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);

		bool playFollowUpStrikeSfx = false;
		while (targetCount > 0)
		{
			if (playFollowUpStrikeSfx)
			{
				SquSfx.Play(SquSfx.TransparentHoleEvent);
			}

			int hits = await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
				this,
				choiceContext,
				targetCount,
				damagePerHit: vigorSequence.ResolveNextAttackDamage(),
				cardPlay: cardPlay);
			if (hits <= 0)
			{
				break;
			}

			int aliveEnemyCount = combatState.HittableEnemies.Count(creature => creature.IsAlive);
			if (aliveEnemyCount >= targetCount)
			{
				break;
			}

			targetCount--;
			playFollowUpStrikeSfx = true;
		}
	}

	private int ResolveTargetCount()
	{
		int targetCount = ResolveEnergyXValue();
		if (IsUpgraded)
		{
			targetCount++;
		}

		return targetCount;
	}
}
