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
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 过禽论：给予目标再生；若目标为敌人则获得活力，否则消耗本牌。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "monster_hunting_method")]
public sealed class MonsterHuntingMethod : ModCardTemplate
{
	public const decimal BaseRegen = 3m;
	public const decimal UpgradedRegen = 4m;
	public const decimal BaseVigor = 6m;
	public const decimal UpgradedVigor = 10m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(BaseRegen),
		new PowerVar<VigorPower>(BaseVigor),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>(),
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/MonsterHuntingMethod.png");

	public MonsterHuntingMethod()
		: base(1, CardType.Skill, CardRarity.Rare, CustomTargetType.Anyone)
	{
	}

	/// <summary>
	/// 结果牌堆在 OnPlay 之前就已决定，不能依赖 ExhaustOnNextPlay。
	/// </summary>
	protected override CardLocation GetResultLocationForCardPlay()
	{
		if (CurrentTarget is { Side: not CombatSide.Enemy })
		{
			return new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom);
		}

		return base.GetResultLocationForCardPlay();
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		Creature target = cardPlay.Target;

		PlayTargetSfx(target);

		await PowerCmd.Apply<RegenPower>(
			choiceContext,
			target,
			DynamicVars[nameof(RegenPower)].BaseValue,
			Owner.Creature,
			this);

		if (target.Side == CombatSide.Enemy)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner.Creature,
				DynamicVars[nameof(VigorPower)].BaseValue,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(RegenPower)].UpgradeValueBy(UpgradedRegen - BaseRegen);
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
	}

	private void PlayTargetSfx(Creature target)
	{
		if (target.Side == CombatSide.Enemy)
		{
			SquSfx.Play(SquSfx.MonsterHuntingMethodEnemyEvent);
		}
		else if (target == Owner.Creature)
		{
			SquSfx.Play(SquSfx.MonsterHuntingMethodSelfEvent);
		}
		else
		{
			SquSfx.Play(SquSfx.MonsterHuntingMethodOtherEvent);
		}
	}
}
