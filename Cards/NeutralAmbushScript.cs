using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 将原版 <see cref="GiantRock"/> 与 <see cref="SalvoStrike"/> 洗入抽牌堆，
/// 并在剧本存续期间于抽到二者之一时自动对随机目标打出。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "neutral_ambush_script")]
public sealed class NeutralAmbushScript : ScriptCardTemplate
{
	public const int BoulderCount = 1;
	public const int SalvoStrikeCount = 2;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(IsUpgraded),
		..HoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(IsUpgraded),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NeutralAmbushScript.png");

	public NeutralAmbushScript()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player player = Owner;
		ICombatState combatState = player.Creature.CombatState
			?? throw new System.InvalidOperationException("NeutralAmbushScript requires an active combat.");

		await GeneratedCombatCards.AddToDrawPileInCombat<GiantRock>(
			combatState,
			player,
			BoulderCount,
			IsUpgraded,
			player);

		await GeneratedCombatCards.AddToDrawPileInCombat<SalvoStrike>(
			combatState,
			player,
			SalvoStrikeCount,
			IsUpgraded,
			player);

		await PowerCmd.Apply<ScriptNeutralAmbushPower>(
			choiceContext,
			player.Creature,
			1m,
			player.Creature,
			this);
	}
}
