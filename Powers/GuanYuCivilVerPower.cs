using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>文关羽：每打出一张技能牌，获得受到敏捷加成的格挡。</summary>
[RegisterPower]
public sealed class GuanYuCivilVerPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public bool FormUpgraded { get; private set; }

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public void SetFormUpgraded(bool upgraded) => FormUpgraded = upgraded;

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0
			|| !cardPlay.IsLastInSeries
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.Card.Type != CardType.Skill)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay: null);
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		await GuanDiFormChoice.OfferRechoiceAsync(this, combatState);
	}
}
