using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "chicken_foot_cheese")]
public sealed class ChickenFootCheese : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 12;
	public const int BaseTurns = 2;
	public const int UpgradedTurns = 3;
	public const int RandomEnemyTargetCount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<ChickenFootCheeseStrikePower>(BaseTurns),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ChickenFootCheese.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public ChickenFootCheese()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => RandomEnemyTargetCount;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (Creature target in SquRandomEnemyTargeting.GetTargets(this, cardPlay.Target))
		{
			if (!target.IsAlive)
			{
				continue;
			}

			await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
				.FromCard(this)
				.Targeting(target)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
		}

		await PowerCmd.Apply<ChickenFootCheeseStrikePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(ChickenFootCheeseStrikePower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars[nameof(ChickenFootCheeseStrikePower)].UpgradeValueBy(UpgradedTurns - BaseTurns);
	}
}
