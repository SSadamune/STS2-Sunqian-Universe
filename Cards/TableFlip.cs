using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool), StableEntryStem = "table_flip")]
public sealed class TableFlip : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(2),
		new DamageVar(18m, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TableFlip.png");

	public TableFlip()
		: base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(WeakPower)].BaseValue,
			Owner.Creature,
			this);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);

		if ((cardPlay.Target.GetPower<WeakPower>()?.Amount ?? 0) <= 0)
		{
			return;
		}

		await TroubleAgainPower.ApplyTrackingAsync(
			choiceContext,
			Owner.Creature,
			this,
			cardPlay.Target);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
	}
}
