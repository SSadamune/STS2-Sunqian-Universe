using System;
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
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 阻拦救火：仅能对已有灼烧的敌人打出；给予灼烧并使其在数回合内不熄灭，然后自己获得虚弱。
/// 无灼烧敌人时不可打出，对齐原版 <c>BubbleBubble</c> 的条件打出方式。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "block_firefighting")]
public sealed class BlockFirefighting : ModCardTemplate
{
	public const decimal WeakAmount = 1m;
	public const decimal BaseBurning = 7m;
	public const decimal UpgradedBurning = 9m;
	public const decimal BaseUnextinguished = 2m;
	public const decimal UpgradedUnextinguished = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BaseBurning),
		new PowerVar<WeakPower>(WeakAmount),
		new PowerVar<UnextinguishedPower>(BaseUnextinguished),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<UnextinguishedPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BlockFirefighting.png");

	public override TargetType TargetType => SquTargetTypes.AnyBurningEnemy;

	protected override bool IsPlayable =>
		CombatState?.HittableEnemies.Any(static enemy => enemy.HasPower<BurningPower>()) ?? false;

	protected override bool ShouldGlowGoldInternal => IsPlayable;

	public BlockFirefighting()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		SquSfx.Play(IsUpgraded
			? SquSfx.BlockFirefightingSageEvent
			: SquSfx.BlockFirefightingDoNotDisturbEvent);

		if (cardPlay.Target.HasPower<BurningPower>())
		{
			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				cardPlay.Target,
				DynamicVars[nameof(BurningPower)].BaseValue,
				Owner.Creature,
				this);

			await PowerCmd.Apply<UnextinguishedPower>(
				choiceContext,
				cardPlay.Target,
				DynamicVars[nameof(UnextinguishedPower)].BaseValue,
				Owner.Creature,
				this);
		}

		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(WeakPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
		DynamicVars[nameof(UnextinguishedPower)].UpgradeValueBy(UpgradedUnextinguished - BaseUnextinguished);
	}
}
