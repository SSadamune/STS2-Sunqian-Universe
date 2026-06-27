using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 将原版 <see cref="GiantRock"/>（巨岩，由 PRIMAL_FORCE 生成）洗入抽牌堆，并在剧本存续期间于抽到巨岩时自动打出。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "neutral_ambush_script")]
public sealed class NeutralAmbushScript : ScriptCardTemplate
{
	public const int BoulderCount = 3;

	private const int NextTurnDrawAmount = 0;

	private const int UpgradedNextTurnDrawAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BoulderCount),
		new PowerVar<DrawCardsNextTurnPower>(NextTurnDrawAmount),
	];

	// 对齐家丁剧本：悬停预览实际生成的 GIANT_ROCK（含升级态）。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(IsUpgraded),
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

		int nextTurnDraw = IsUpgraded ? UpgradedNextTurnDrawAmount : NextTurnDrawAmount;
		await PowerCmd.Apply<DrawCardsNextTurnPower>(
			choiceContext,
			player.Creature,
			nextTurnDraw,
			player.Creature,
			this);

		await PowerCmd.Apply<ScriptNeutralAmbushPower>(
			choiceContext,
			player.Creature,
			1m,
			player.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(DrawCardsNextTurnPower)].UpgradeValueBy(1m);
	}
}
