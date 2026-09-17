using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 何疑魏：造成伤害并获得力量；因虚无被消耗后获得虚弱。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "why_doubt_wei")]
public sealed class WhyDoubtWei : ModCardTemplate
{
	public const decimal BaseDamage = 15m;
	public const decimal UpgradedDamage = 19m;
	public const decimal BaseStrength = 2m;
	public const decimal UpgradedStrength = 3m;
	public const decimal WeakAmount = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<StrengthPower>(BaseStrength),
		new PowerVar<WeakPower>(WeakAmount),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Ethereal,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WhyDoubtWei.png");

	public WhyDoubtWei()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		await PowerCmd.Apply<StrengthPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(StrengthPower)].BaseValue,
			Owner.Creature,
			this);
	}

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card != this || !causedByEthereal || CombatState == null)
		{
			return;
		}

		SquSfx.Play(SquSfx.WhyDoubtWeiEvent);
		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(WeakPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars[nameof(StrengthPower)].UpgradeValueBy(UpgradedStrength - BaseStrength);
	}
}
