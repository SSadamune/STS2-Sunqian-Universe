using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 欲擒故纵：从抽牌堆随机选出若干张牌并丢弃；这些牌完成全部弃牌插入结算
/// （包括[gold]奇巧[/gold]自动打出）后，选择其中一张获得；跳过选择则在下回合
/// 正常抽牌前获得仍在抽牌堆或弃牌堆的候选牌。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "lure_and_release")]
public sealed class LureAndRelease : ModCardTemplate
{
	public const int BaseCandidateCount = 3;
	public const int UpgradedCandidateCount = 4;

	internal const string SelectionPromptKey =
		"SUNQIAN_UNIVERSE_CARD_LURE_AND_RELEASE.selectionScreenPrompt";

	private static readonly LocString SelectionPrompt =
		new("cards", SelectionPromptKey);

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseCandidateCount),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/PrisonerPlaceholder.png");

	public LureAndRelease()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int candidateCount = DynamicVars.Cards.IntValue;
		if (candidateCount <= 0)
		{
			return;
		}

		List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
			.TakeRandom(candidateCount, Owner.RunState.Rng.CombatCardSelection)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		// 先完成全部弃牌及其插入结算（AfterCardDiscarded、奇巧自动打出），
		// 再基于候选牌当前所在区域决定本次选牌界面。CardCmd.Discard 是正式
		// 弃牌入口，会等所有奇巧自动打出结束后才返回。
		await CardCmd.Discard(choiceContext, candidates);
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}

		List<CardModel> selectable = GetPileEligibleCards(candidates);
		if (selectable.Count == 0)
		{
			return;
		}

		CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
			choiceContext,
			selectable,
			Owner,
			new CardSelectorPrefs(SelectionPrompt, 0, 1))).FirstOrDefault();

		if (selected != null)
		{
			await CardPileCmd.Add(selected, PileType.Hand);
			return;
		}

		// 跳过选择时保留原始候选身份；下回合触发时再按当时的区域过滤，
		// 因此中途离开抽牌堆/弃牌堆的牌不会成为操作对象。
		await LureAndReleasePower.ApplyAsync(choiceContext, Owner, candidates, this);
	}

	/// <summary>
	/// 选牌与延迟获得共同的操作对象判定：只认仍在抽牌堆或弃牌堆的实体牌。
	/// </summary>
	internal static List<CardModel> GetPileEligibleCards(IEnumerable<CardModel> cards) =>
		cards.Where(IsInDrawOrDiscardPile).ToList();

	internal static bool IsInDrawOrDiscardPile(CardModel card) =>
		card.Pile?.Type is PileType.Draw or PileType.Discard;

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedCandidateCount - BaseCandidateCount);
	}
}
