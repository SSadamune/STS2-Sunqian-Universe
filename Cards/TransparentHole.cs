using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "transparent_hole")]
public sealed class TransparentHole : ModCardTemplate, IRandomEnemyTargetCount, IAttackHitHookListener
{
	public const int BaseDamage = 7;

	private const float RepeatAttackDelaySeconds = 0.2f;

	private AttackCommand? _cascadeAttack;

	private int _cascadeInitialCount;

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

		List<Creature> firstVolley = SquRandomEnemyTargeting.SelectRandomEnemies(this, targetCount);
		if (firstVolley.Count == 0)
		{
			return;
		}

		AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(combatState)
			.TargetingFiltered(firstVolley)
			.WithHitCount(targetCount)
			.WithHitFx("vfx/vfx_attack_slash");

		_cascadeAttack = attack;
		_cascadeInitialCount = targetCount;
		try
		{
			await attack.Execute(choiceContext);
		}
		finally
		{
			_cascadeAttack = null;
		}
	}

	public Task BeforeAttackHit(AttackHitContext context)
	{
		if (context.Attack != _cascadeAttack)
		{
			return Task.CompletedTask;
		}

		PlayTransparentHoleSfx();
		return Task.CompletedTask;
	}

	public async Task AfterAttackHit(AttackHitContext context)
	{
		if (context.Attack != _cascadeAttack)
		{
			return;
		}

		int previousTargetCount = _cascadeInitialCount - context.HitIndex;
		int nextTargetCount = previousTargetCount - 1;
		int aliveEnemyCount = context.CombatState.HittableEnemies.Count(creature => creature.IsAlive);
		if (nextTargetCount <= 0 || aliveEnemyCount >= previousTargetCount)
		{
			context.Attack.TargetingFiltered([]);
			return;
		}

		context.Attack.TargetingFiltered(
			SquRandomEnemyTargeting.SelectRandomEnemies(this, nextTargetCount));
		await Cmd.Wait(RepeatAttackDelaySeconds);
	}

	private void PlayTransparentHoleSfx()
	{
		SquSfx.PlayRandom(
			RunState,
			SquSfx.TransparentHoleGuanYuEvent,
			SquSfx.TransparentHoleZhouYuEvent,
			SquSfx.TransparentHoleMaChaoEvent,
			SquSfx.TransparentHoleLuBuEvent,
			SquSfx.TransparentHoleYuanShuEvent);
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
