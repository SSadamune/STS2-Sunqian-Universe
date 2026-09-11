using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 玉女剑法：造成伤害，并在本回合获得敏捷。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "jade_maiden_swordplay")]
public sealed class JadeMaidenSwordplay : ModCardTemplate
{
	public const decimal CanonicalDamage = 9m;
	public const decimal UpgradedDamage = 12m;
	public const decimal CanonicalDexterity = 2m;
	public const decimal UpgradedDexterity = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, ValueProp.Move),
		new PowerVar<DexterityPower>(CanonicalDexterity),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/JadeMaidenSwordplay.png");

	public JadeMaidenSwordplay()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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

		await PowerCmd.Apply<TempDexFromJadeMaidenSwordplayPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(DexterityPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
		DynamicVars[nameof(DexterityPower)].UpgradeValueBy(UpgradedDexterity - CanonicalDexterity);
	}
}
