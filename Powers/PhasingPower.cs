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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class PhasingPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/PhasingPower.png",
		BigIconPath: "res://images/powers/PhasingPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(Phasing.MinDexVarName, 0),
		new DynamicVar(Phasing.MaxDexVarName, 0),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public int MinBound => (int)DynamicVars[Phasing.MinDexVarName].BaseValue;

	public int MaxBound => (int)DynamicVars[Phasing.MaxDexVarName].BaseValue;

	public void SetBounds(int min, int max)
	{
		DynamicVars[Phasing.MinDexVarName].BaseValue = min;
		DynamicVars[Phasing.MaxDexVarName].BaseValue = max;
	}

	public void AddBounds(int minDelta, int maxDelta) =>
		SetBounds(MinBound + minDelta, MaxBound + maxDelta);

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		int dexterity = RollTemporaryDexterity(Owner.Player, MinBound, MaxBound);

		Flash();

		await ApplyTemporaryDexterityAsync(
			new ThrowingPlayerChoiceContext(),
			Owner,
			Applier,
			null,
			dexterity);
	}

	public static int RollTemporaryDexterity(Player player, int minInclusive, int maxInclusive) =>
		player.RunState.Rng.CombatTargets.NextInt(minInclusive, maxInclusive + 1);

	public static async Task AddOrStackBoundsAsync(
		PlayerChoiceContext choiceContext,
		Creature target,
		int minContribution,
		int maxContribution,
		Creature applier,
		CardModel card)
	{
		PhasingPower existing = target.GetPower<PhasingPower>();
		if (existing == null)
		{
			await PowerCmd.Apply<PhasingPower>(
				choiceContext,
				target,
				1,
				applier,
				card);

			target.GetPower<PhasingPower>()!.SetBounds(minContribution, maxContribution);
			return;
		}

		existing.AddBounds(minContribution, maxContribution);
	}

	/// <summary>
	/// Applies mod temp-dex wrapper. Card source is required when played from a card; pass null from power hooks.
	/// </summary>
	public static Task ApplyTemporaryDexterityAsync(
		PlayerChoiceContext choiceContext,
		Creature target,
		Creature applier,
		CardModel card,
		int amount)
	{
		if (amount <= 0)
		{
			return Task.CompletedTask;
		}

		return TempDexPower.ApplyAsync(
			choiceContext,
			target,
			applier,
			card,
			amount);
	}
}
