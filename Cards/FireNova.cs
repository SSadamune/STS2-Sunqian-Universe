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
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 飞火流星：造成伤害并对所有敌人施加灼烧；升级后变为 X 费，
/// 对至多 X 名互不重复的随机敌人分别执行「造成伤害 + 对所有敌人施加灼烧」。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "fire_nova")]
public sealed class FireNova : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int DamageAmount = 3;
	public const int BurningStacks = 6;

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

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FireNova.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public FireNova()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => IsUpgraded ? ResolveEnergyXValue() : 1;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		int requestedCount = GetRandomEnemyTargetCount();
		if (requestedCount <= 0)
		{
			return;
		}

		List<Creature> damageTargets = SquRandomEnemyTargeting
			.SelectRandomEnemies(this, requestedCount);
		if (damageTargets.Count == 0)
		{
			return;
		}

		SquVigorSnapshot.AttackSequence vigorSequence =
			SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);

		foreach (Creature damageTarget in damageTargets)
		{
			if (!damageTarget.IsAlive)
			{
				continue;
			}

			await DealDamage(
				choiceContext,
				damageTarget,
				cardPlay,
				vigorSequence.ResolveNextAttackDamage());
			await ApplyBurningToAllEnemies(choiceContext, combatState);
		}
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 0, costsX: true));
		InvokeEnergyCostChanged();
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
				DynamicVars[nameof(BurningPower)].BaseValue,
				Owner.Creature,
				this);
		}
	}

	private async Task DealDamage(
		PlayerChoiceContext choiceContext,
		Creature target,
		CardPlay cardPlay,
		decimal? damage = null)
	{
		await DamageCmd.Attack(damage ?? DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}
}
