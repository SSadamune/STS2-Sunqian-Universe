using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 兼职将军剧本：消耗 X 点能量，并用同步战斗随机数从其它角色的牌池中抽取 X 张
/// 非初始 Strike 牌，作为不存在的牌对指定目标自动打出。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "part_time_general_script")]
public sealed class PartTimeGeneralScript : ScriptCardTemplate
{
	protected override bool HasEnergyCostX => true;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		SquKeywords.SupremeGeneral,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.CountsAsPlayed),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/PartTimeGeneralScript.png");

	public PartTimeGeneralScript()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true)
	{
	}

	protected override async Task PlayScriptAsync(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException(
				"PartTimeGeneralScript requires an active combat.");
		Creature target = cardPlay.Target;
		int playCount = ResolveEnergyXValue();
		if (playCount > 0)
		{
			await SupremeGeneralKeywordSystem.ConsumeVigorForRootWithoutAttack(
				choiceContext,
				cardPlay);
		}

		List<CardModel> playedCards = [];
		List<CardModel> strikeCards = ChooseStrikeCards(playCount);
		for (int index = 0; index < strikeCards.Count; index++)
		{
			CardModel canonical = strikeCards[index];
			CardModel source = combatState.CreateCard(canonical, Owner);
			if (IsUpgraded && source.IsUpgradable)
			{
				source.UpgradeInternal();
				source.FinalizeUpgradeInternal();
			}

			CardModel autoPlayedCard = source.CreateDupe(Owner);
			source.RemoveFromState();
			playedCards.Add(autoPlayedCard);
			await CardCmd.AutoPlay(
				choiceContext,
				autoPlayedCard,
				target);
			if (index < strikeCards.Count - 1)
			{
				await Cmd.Wait(0.2f);
			}
		}

		var scriptPower = (ScriptPartTimeGeneralPower)ModelDb
			.Power<ScriptPartTimeGeneralPower>()
			.ToMutable();
		scriptPower.RecordPlayedCards(playedCards);
		await PowerCmd.Apply(
			choiceContext,
			scriptPower,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	private List<CardModel> ChooseStrikeCards(int count)
	{
		if (count <= 0)
		{
			return [];
		}

		List<CardPoolModel> pools = Owner.UnlockState.CharacterCardPools
			.Where(pool => pool.Id != Owner.Character.CardPool.Id)
			.ToList();

		List<CardModel> candidates = pools
			.SelectMany(pool => pool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint))
			.Where(IsEligibleStrike)
			.GroupBy(card => card.Id)
			.Select(group => group.First())
			.ToList();

		Owner.RunState.Rng.CombatCardGeneration.Shuffle(candidates);
		return candidates.Take(Math.Min(count, candidates.Count)).ToList();
	}

	private static bool IsEligibleStrike(CardModel card) =>
		card.Type == CardType.Attack
		&& card.Tags.Contains(CardTag.Strike)
		&& card.Rarity != CardRarity.Basic;
}
