using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Squ.Audio;
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

	private const float FirstStrikeLeadInSeconds = 1.2f;
	private const float StrikeIntervalSeconds = 0.15f;
	private const float StrikeShakeDelaySeconds = 0.4f;

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
		List<CardModel> playedCards = [];
		List<CardModel> strikeCards = ChooseStrikeCards(playCount);
		bool willPlayStrikes = strikeCards.Count > 0;
		NPartTimeGeneralFinaleVfx? finale = null;
		if (willPlayStrikes)
		{
			await SupremeGeneralKeywordSystem.ConsumeVigorForRootWithoutAttack(
				choiceContext,
				cardPlay);
			SquSfx.Play(SquSfx.PartTimeGeneralShadowCloneEvent);
			finale = NPartTimeGeneralFinaleVfx.Create(Owner.Creature);
		}

		try
		{
			if (willPlayStrikes)
			{
				await Cmd.Wait(FirstStrikeLeadInSeconds);
			}

			for (int index = 0; index < strikeCards.Count; index++)
			{
				_ = TaskHelper.RunSafely(PlayStrikeShakeAsync());
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
					await Cmd.Wait(StrikeIntervalSeconds);
				}
			}
		}
		finally
		{
			finale?.PlayOutro();
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

	private static async Task PlayStrikeShakeAsync()
	{
		await Cmd.Wait(StrikeShakeDelaySeconds);
		NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Normal);
	}

	private List<CardModel> ChooseStrikeCards(int count)
	{
		if (count <= 0)
		{
			return [];
		}

		IEnumerable<CardModel> cards = CharacterCardPoolMembership.GetUnlockedCards(
			Owner,
			CharacterCardPoolMembership.GetOtherCharacterPools(Owner));
		List<CardModel> candidates = CardFactory.FilterForCombat(cards)
			.Where(IsEligibleStrike)
			.ToList();

		Owner.RunState.Rng.CombatCardGeneration.Shuffle(candidates);
		return candidates.Take(Math.Min(count, candidates.Count)).ToList();
	}

	private static bool IsEligibleStrike(CardModel card) =>
		card.Type == CardType.Attack
		&& card.Tags.Contains(CardTag.Strike);
}
