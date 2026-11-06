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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 当浮一大白：每层让当前存在的本人二弟、三弟从每张酒获得等量活力的倍率。
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
	}
}
