using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 飞火流星：对随机敌人造成伤害并施加灼烧。
/// 右键「稍作修改」后，变为旧版升级效果的火焰流星雨。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "fire_nova")]
public sealed class FireNova : ModCardTemplate, IRandomEnemyTargetCount, IModRightClickableCard
{
	public const int DamageAmount = 4;
	public const int BurningStacks = 5;
	public const int UpgradedDamageAmount = 6;
	public const int UpgradedBurningStacks = 7;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new PowerVar<BurningPower>(BurningStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		..HoverTipFactory.FromCardWithCardHoverTips<FireMeteorShower>(IsUpgraded),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.SlightRevision,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FireNova.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public FireNova()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => 1;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<Creature> targets = SquRandomEnemyTargeting.SelectRandomEnemies(this, 1);
		if (targets.Count == 0)
		{
			return;
		}

		Creature target = targets[0];
		SquVigorSnapshot.AttackSequence vigorSequence =
			SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);
		SquSfx.PlayRandom(RunState, SquSfx.FlyingFireMeteorEvents);
		await DamageCmd.Attack(vigorSequence.ResolveNextAttackDamage())
			.FromCard(this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
		foreach (Creature burningTarget in CombatState!.HittableEnemies)
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

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurningStacks - BurningStacks);
	}

	public bool CanHandleRightClickLocal(ModRightClickContext context) =>
		context.Player == Owner
		&& Pile?.Type == PileType.Hand
		&& IsTransformable;

	public async Task OnRightClick(ModRightClickExecutionContext context)
	{
		if (Pile?.Type != PileType.Hand || !IsTransformable || CardScope is not { } cardScope)
		{
			return;
		}

		CardModel replacement = cardScope.CreateCard<FireMeteorShower>(Owner);
		if (IsUpgraded)
		{
			replacement.UpgradeInternal();
			replacement.FinalizeUpgradeInternal();
		}

		await CardCmd.Transform(this, replacement);
	}
}
