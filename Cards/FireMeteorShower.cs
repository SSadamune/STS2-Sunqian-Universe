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
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>由飞火流星的「稍作修改」产生，沿用其改版前的升级效果。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "fire_meteor_shower")]
public sealed class FireMeteorShower : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int DamageAmount = 4;
	public const int BurningStacks = 5;
	public const int UpgradedDamageAmount = 6;
	public const int UpgradedBurningStacks = 7;

	protected override bool HasEnergyCostX => true;

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

	public FireMeteorShower()
		: base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

		List<Creature> damageTargets = SquRandomEnemyTargeting
			.SelectRandomEnemies(this, GetRandomEnemyTargetCount());
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

			SquSfx.PlayRandom(RunState, SquSfx.FlyingFireMeteorEvents);
			await DamageCmd.Attack(vigorSequence.ResolveNextAttackDamage())
				.FromCard(this, cardPlay)
				.Targeting(damageTarget)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);

			foreach (Creature burningTarget in combatState.HittableEnemies)
			{
				if (!burningTarget.IsAlive)
				{
					continue;
				}

				await PowerCmd.Apply<BurningPower>(
					choiceContext,
					burningTarget,
					DynamicVars[nameof(BurningPower)].BaseValue,
					Owner.Creature,
					this);
			}
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(
			UpgradedBurningStacks - BurningStacks);
	}
}
