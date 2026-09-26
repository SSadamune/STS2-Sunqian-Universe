#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>吾亦过江：获得活力与易伤，并在敌人准备攻击时暂时使攻击牌获得双倍活力加成。</summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "cross_the_river")]
public sealed class CrossTheRiver : ModCardTemplate
{
	public const int BaseVigor = 7;
	public const int UpgradedVigor = 9;
	public const int VulnerableAmount = 1;
	public const int BaseDuration = 2;
	public const int UpgradedDuration = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
		new PowerVar<VulnerablePower>(VulnerableAmount),
		new PowerVar<CrossTheRiverPower>(BaseDuration),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CrossTheRiver.png");

	protected override bool ShouldGlowGoldInternal => HasAttackingEnemy();

	public CrossTheRiver()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.CrossTheRiverPlayEvent);
		Creature owner = Owner.Creature;
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			owner,
			DynamicVars[nameof(VigorPower)].BaseValue,
			owner,
			this);
		await PowerCmd.Apply<VulnerablePower>(
			choiceContext,
			owner,
			DynamicVars[nameof(VulnerablePower)].BaseValue,
			owner,
			this);

		if (HasAttackingEnemy())
		{
			await PowerCmd.Apply<CrossTheRiverPower>(
				choiceContext,
				owner,
				DynamicVars[nameof(CrossTheRiverPower)].BaseValue,
				owner,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
		DynamicVars[nameof(CrossTheRiverPower)]
			.UpgradeValueBy(UpgradedDuration - BaseDuration);
	}

	private bool HasAttackingEnemy() =>
		CombatState?.HittableEnemies.Any(enemy =>
			enemy.IsAlive && SquEnemyIntent.IntendsToAttack(enemy)) == true;
}
