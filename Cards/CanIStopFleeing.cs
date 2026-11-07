#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "can_i_stop_fleeing")]
public sealed class CanIStopFleeing : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StopRunningPower>(1)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/CanIStopFleeing.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[IsUpgraded ? HoverTipFactory.FromPower<StopFleeingUntilEnemyTurnEndPower>() : HoverTipFactory.FromPower<StopRunningPower>(), HoverTipFactory.FromPower<TemporaryHealthPower>()];
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
	public CanIStopFleeing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		if (IsUpgraded)
			await PowerCmd.Apply<StopFleeingUntilEnemyTurnEndPower>(context, Owner.Creature, 1, Owner.Creature, this);
		else
			await PowerCmd.Apply<StopRunningPower>(context, Owner.Creature, 1, Owner.Creature, this);
	}
}
