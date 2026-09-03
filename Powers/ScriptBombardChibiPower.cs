using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using Squ.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptBombardChibiPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBombardChibiPower.png",
		BigIconPath: "res://images/powers/ScriptBombardChibiPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BombardChibiScript.BaseBurning),
	];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		if (cardSource is BombardChibiScript card)
		{
			DynamicVars[nameof(BurningPower)].BaseValue =
				card.DynamicVars[nameof(BurningPower)].BaseValue;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		decimal strengthLoss = DynamicVars[nameof(BurningPower)].BaseValue;
		ICombatState? combatState = oldOwner.CombatState;
		if (strengthLoss > 0 && combatState is not null)
		{
			SquSfx.Play(SquSfx.BombardChibiReduceAttackEvent);
			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (!enemy.IsAlive)
				{
					continue;
				}

				await PowerCmd.Apply<BombardChibiStrengthDownPower>(
					new ThrowingPlayerChoiceContext(),
					enemy,
					strengthLoss,
					oldOwner,
					null);
			}
		}

		await base.AfterRemoved(oldOwner);
	}
}
