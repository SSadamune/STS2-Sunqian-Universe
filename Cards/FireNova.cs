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
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// Fire Nova: attack that applies Burning to all enemies; when upgraded, also plays an Exhausting
/// unupgraded combat copy on each enemy.
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "fire_nova")]
public sealed class FireNova : ModCardTemplate
{
	public const int DamageAmount = 8;
	public const int BurningStacks = 3;

	private bool _isUnupgradablePlusSpawnedCopy;

	public override int MaxUpgradeLevel => _isUnupgradablePlusSpawnedCopy ? 0 : 1;

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

		await DealDamage(choiceContext, cardPlay.Target);

		if (IsUpgraded && !_isUnupgradablePlusSpawnedCopy)
		{
			foreach (Creature target in combatState.HittableEnemies)
			{
				if (!target.IsAlive)
				{
					continue;
				}

				CardModel copy = CreateExhaustingCombatCopy(combatState);
				await CardCmd.AutoPlay(choiceContext, copy, target);
			}
		}
		else
		{
			await ApplyBurningToAllEnemies(choiceContext, combatState);
		}
	}

	protected override void OnUpgrade()
	{
		if (_isUnupgradablePlusSpawnedCopy)
		{
			return;
		}

		EnergyCost.UpgradeBy(1);
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

	private CardModel CreateExhaustingCombatCopy(ICombatState combatState)
	{
		FireNova copy = combatState.CreateCard<FireNova>(Owner);
		copy.MarkAsUnupgradablePlusSpawnedCopy();
		copy.AddKeyword(CardKeyword.Exhaust);
		return copy;
	}

	private void MarkAsUnupgradablePlusSpawnedCopy()
	{
		_isUnupgradablePlusSpawnedCopy = true;
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
