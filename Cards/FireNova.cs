using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// Fire Nova: deals damage and applies Burning to all enemies; when upgraded, becomes X-cost and
/// repeats that effect on X random enemies including the target.
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "fire_nova")]
public sealed class FireNova : ModCardTemplate
{
	public const int DamageAmount = 3;
	public const int BurningStacks = 5;

	protected override bool HasEnergyCostX => IsUpgraded;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new PowerVar<BurningPower>(BurningStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FireNova.png");

	public FireNova()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		if (IsUpgraded)
		{
			int playCount = ResolveEnergyXValue();
			if (playCount <= 0)
			{
				return;
			}

			foreach (Creature target in PickRandomEnemiesIncluding(
				combatState,
				cardPlay.Target,
				playCount,
				combatState.RunState.Rng.CombatTargets))
			{
				await ExecuteBaseEffect(choiceContext, combatState, target);
			}
		}
		else
		{
			await ExecuteBaseEffect(choiceContext, combatState, cardPlay.Target);
		}
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 0, costsX: true));
		InvokeEnergyCostChanged();
	}

	private async Task ExecuteBaseEffect(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		Creature damageTarget)
	{
		await DealDamage(choiceContext, damageTarget);
		await ApplyBurningToAllEnemies(choiceContext, combatState);
	}

	private static IEnumerable<Creature> PickRandomEnemiesIncluding(
		ICombatState combatState,
		Creature mustInclude,
		int count,
		Rng rng)
	{
		if (count <= 0)
		{
			yield break;
		}

		List<Creature> alive = combatState.HittableEnemies
			.Where(creature => creature.IsAlive)
			.ToList();
		if (alive.Count == 0 || !mustInclude.IsAlive || !alive.Contains(mustInclude))
		{
			yield break;
		}

		int pickCount = Math.Min(count, alive.Count);
		HashSet<Creature> selected = [mustInclude];
		List<Creature> pool = alive.Where(creature => creature != mustInclude).ToList();

		for (int i = pool.Count - 1; i > 0; i--)
		{
			int swapIndex = rng.NextInt(0, i + 1);
			(pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
		}

		foreach (Creature creature in pool.Take(pickCount - 1))
		{
			selected.Add(creature);
		}

		foreach (Creature creature in selected)
		{
			yield return creature;
		}
	}

	private async Task ApplyBurningToAllEnemies(PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		foreach (Creature target in combatState.HittableEnemies)
		{
			if (!target.IsAlive)
			{
				continue;
			}

			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				target,
				BurningStacks,
				Owner.Creature,
				this);
		}
	}

	private async Task DealDamage(PlayerChoiceContext choiceContext, Creature target)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}
}
