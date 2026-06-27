using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "table_flip")]
public sealed class TableFlip : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(2),
		new DamageVar(16m, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TableFlip.png");

	public TableFlip()
		: base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		ArgumentNullException.ThrowIfNull(combatState, nameof(combatState));

		decimal weakAmount = DynamicVars[nameof(WeakPower)].BaseValue;
		foreach (Creature target in combatState.HittableEnemies)
		{
			if (!target.IsAlive)
			{
				continue;
			}

			await PowerCmd.Apply<WeakPower>(
				choiceContext,
				target,
				weakAmount,
				Owner.Creature,
				this);
		}

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.TargetingAllOpponents(combatState)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);

		await TroubleAgainPower.ApplyTrackingAsync(choiceContext, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		MockSetEnergyCost(new CardEnergyCost(this, 2, costsX: false));
		InvokeEnergyCostChanged();
	}
}
