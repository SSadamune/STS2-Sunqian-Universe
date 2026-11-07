using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 不可饮酒致醉：生成对应升级状态的酒，并赋予饮酒保护能力。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "cannot_get_drunk")]
public sealed class CannotGetDrunk : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquCardHoverTipFactory.FromCard<Wine>(this, IsUpgraded),
		HoverTipFactory.FromCard<Dazed>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/prisoner/CannotGetDrunk.png");

	public CannotGetDrunk()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("CannotGetDrunk requires combat.");
		await GeneratedCombatCards.AddToDrawPileInCombat<Wine>(
			combatState,
			Owner,
			1,
			upgraded: IsUpgraded,
			Owner);
		await PowerCmd.Apply<CannotGetDrunkPower>(
			choiceContext,
			Owner.Creature,
			1,
			Owner.Creature,
			this);
	}
}
