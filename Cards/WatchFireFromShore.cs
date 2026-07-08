using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "watch_fire_from_shore")]
public sealed class WatchFireFromShore : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseBlock = 7;
	public const int UpgradedBlock = 9;
	public const int BaseBurning = 2;
	public const int UpgradedBurning = 3;
	public const int RandomEnemyTargetCount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BaseBlock, ValueProp.Move),
		new PowerVar<BurningPower>(BaseBurning),
	];

	public override bool GainsBlock => true;

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WatchFireFromShore.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public WatchFireFromShore()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => RandomEnemyTargetCount;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		decimal burningStacks = DynamicVars[nameof(BurningPower)].BaseValue;
		foreach (Creature target in SquRandomEnemyTargeting.PickRandomEnemiesUnique(
			combatState,
			RandomEnemyTargetCount,
			Owner.RunState.Rng.CombatTargets))
		{
			if (!target.IsAlive)
			{
				continue;
			}

			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				target,
				burningStacks,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}
}
