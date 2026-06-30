using System;
using System.Collections.Generic;
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
/// Fire Nova: deals damage and applies Burning to all enemies; when upgraded, becomes X-cost and
/// executes that effect once each on up to X distinct random enemies.
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

	public override TargetType TargetType =>
		IsUpgraded ? SquTargetTypes.RandomEnemies : TargetType.AnyEnemy;

	public FireNova()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

		if (IsUpgraded)
		{
			int hitCount = GetRandomEnemyTargetCount();
			if (hitCount <= 0)
			{
				return;
			}

			int damageHits = await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
				this,
				choiceContext,
				hitCount);

			for (int i = 0; i < damageHits; i++)
			{
				await ApplyBurningToAllEnemies(choiceContext, combatState);
			}

			return;
		}

		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		await ExecuteBaseEffect(choiceContext, combatState, cardPlay.Target);
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
