using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class HujinHuyuanPower : ModPowerTemplate
{
	public const int BaseMaxDexterity = 3;
	public const int UpgradedMaxDexterity = 5;
	public const int DebugFixedTempDexterity = 2;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/HujinHuyuanPower.png",
		BigIconPath: "res://images/powers/HujinHuyuanPower.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		// int maxDexterity = (int)Amount;
		// int dexterity = RollTemporaryDexterity(Owner.Player, maxDexterity);
		// if (dexterity <= 0)
		// {
		// 	return;
		// }
		int dexterity = DebugFixedTempDexterity;

		Flash();

		await ApplyTemporaryDexterityLikeAnticipateAsync(
			new ThrowingPlayerChoiceContext(),
			Owner,
			Applier,
			null,
			dexterity);
	}

	// public static int RollTemporaryDexterity(Player player, int maxInclusive) =>
	// 	player.RunState.Rng.CombatTargets.NextInt(0, maxInclusive + 1);

	/// <summary>
	/// Matches vanilla Anticipate: applies AnticipatePower, which grants Dexterity until end of turn.
	/// </summary>
	public static Task ApplyTemporaryDexterityLikeAnticipateAsync(
		PlayerChoiceContext choiceContext,
		Creature target,
		Creature applier,
		CardModel card,
		int amount) =>
		PowerCmd.Apply<AnticipatePower>(
			choiceContext,
			target,
			amount,
			applier,
			card);
}
