using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Patches;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 当浮一大白：酒为本人的兄弟提供活力，多人模式也分享给其他玩家。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "when_float_one_cup")]
public sealed class WhenFloatOneCup : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquCardHoverTipFactory.FromCard<Wine>(false),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/prisoner/WhenFloatOneCup.png");

	public WhenFloatOneCup()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new System.InvalidOperationException("WhenFloatOneCup requires combat.");
		await GeneratedCombatCards.AddToHandInCombat<Wine>(
			combatState,
			Owner,
			upgraded: false,
			Owner);
		await PowerCmd.Apply<WhenFloatOneCupPower>(
			choiceContext,
			Owner.Creature,
			IsUpgraded && Owner.RunState.Players.Count == 1 ? 2 : 1,
			Owner.Creature,
			this);
	}

	protected override void AddExtraArgsToDescription(LocString description) =>
		description.Add("IsMultiplayer", IsMutable && Owner is { } owner
			? owner.RunState.Players.Count > 1
			: CardLibraryMultiplayerPreviewState.ShowMultiplayerCards);
}
