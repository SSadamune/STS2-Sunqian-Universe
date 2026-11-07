#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Interop;
using Squ.Patches;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 当浮一大白：按层数将酒的活力分享给本人的兄弟，多人模式也分享给其他玩家。
/// </summary>
[RegisterPower]
public sealed class WhenFloatOneCupPower : ModPowerTemplate
{
	private static readonly string[] Brothers =
	[
		CompanionSummons.SecondBrotherId,
		CompanionSummons.ThirdBrotherId,
	];

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/VigorAmplificationPower.png",
		BigIconPath: "res://images/powers/VigorAmplificationPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			description.Add("Amount", Amount);
			description.Add("IsMultiplayer", IsMutable && Owner?.Player is { } player
				? player.RunState.Players.Count > 1
				: CardLibraryMultiplayerPreviewState.ShowMultiplayerCards);
			return description;
		}
	}

	public override async Task AfterCardPlayed(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (cardPlay.Card is not Wine wine
			|| wine.Owner?.Creature != Owner
			|| Owner.Player is not { } player)
		{
			return;
		}

		decimal vigor = wine.DynamicVars[nameof(VigorPower)].BaseValue;
		foreach (string id in Brothers)
		{
			Creature? brother = CompanionSummons.GetCompanion(player, id);
			if (brother is not { IsAlive: true })
			{
				continue;
			}

			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				brother,
				vigor * Amount,
				Owner,
				wine);
		}

		foreach (var ally in player.RunState.Players)
		{
			if (ally == player || !ally.Creature.IsAlive
				|| ally.Creature.CombatState != Owner.CombatState)
			{
				continue;
			}
			await NewsanguoVigorSwap.ApplyVigorOrDrunkenMight(
				choiceContext, ally.Creature, vigor * Amount, Owner, wine);
		}
	}
}
