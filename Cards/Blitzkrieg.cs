using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 闪电战：对目标造成伤害，再从手牌与抽牌堆对该敌人打出所有闪电战与打击。
/// 升级获得「契合」（参考原版 PerfectFit：非初始洗牌时置顶）。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "blitzkrieg")]
public sealed class Blitzkrieg : ModCardTemplate
{
	public const int BaseDamage = 8;

	public const int UpgradedDamage = 10;

	private const float AutoPlayDelaySeconds = 0.2f;

	/// <summary>
	/// 防止同一张实例在尚未结算完时被再次拉入 OnPlay（例如洗牌回抽牌堆后的嵌套 AutoPlay）造成无限循环。
	/// 不同实例之间的递归打出是允许的。
	/// </summary>
	private static readonly HashSet<CardModel> Resolving = [];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		IsUpgraded ? [HoverTipFactory.FromKeyword(SquKeywords.Fit)] : [];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.WarFeedsWar];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Blitzkrieg.png");

	public Blitzkrieg()
		: base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		if (!Resolving.Add(this))
		{
			return;
		}

		WarFeedsWarResolutionTracker.RecordPlayed(this);

		try
		{
			if (!cardPlay.IsAutoPlay)
			{
				SquSfx.Play(SquSfx.BlitzkriegThreeDaysEightHundredLiEvent);
			}

			await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
				.FromCard(this, cardPlay)
				.Targeting(cardPlay.Target)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);

			await PlayAllStrikesAndBlitzkriegFromPiles(choiceContext, cardPlay.Target);
		}
		finally
		{
			Resolving.Remove(this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		AddKeyword(SquKeywords.Fit);
	}

	/// <summary>
	/// Mirrors vanilla <see cref="MegaCrit.Sts2.Core.Models.Enchantments.PerfectFit"/>.
	/// </summary>
	public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
	{
		if (!IsUpgraded || isInitialShuffle || player != Owner)
		{
			return;
		}

		if (!cards.Contains(this))
		{
			return;
		}

		cards.Remove(this);
		cards.Insert(0, this);
	}

	private async Task PlayAllStrikesAndBlitzkriegFromPiles(
		PlayerChoiceContext choiceContext,
		Creature target)
	{
		List<CardModel> toPlay =
		[
			.. PileType.Hand.GetPile(Owner).Cards.Where(ShouldAutoPlay),
			.. PileType.Draw.GetPile(Owner).Cards.Where(ShouldAutoPlay),
		];

		foreach (CardModel card in toPlay)
		{
			if (!target.IsAlive)
			{
				break;
			}

			PileType? pileType = card.Pile?.Type;
			if (pileType is not PileType.Hand and not PileType.Draw)
			{
				continue;
			}

			await Cmd.Wait(AutoPlayDelaySeconds);
			PlayAutoPlaySfx();
			await CardCmd.AutoPlay(choiceContext, card, target);
		}
	}

	/// <summary>
	/// 可自动打出：其它「闪电战」，或基础「打击」
	/// （与原版 <c>GhostSeed</c> 一致：<c>Rarity.Basic</c> + <c>CardTag.Strike</c>）。
	/// </summary>
	private static bool ShouldAutoPlay(CardModel card) =>
		card is Blitzkrieg
		|| (card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike));

	private void PlayAutoPlaySfx()
	{
		SquSfx.PlayRandom(
			RunState,
			SquSfx.BlitzkriegRushToLujiangEvent,
			SquSfx.BlitzkriegConsecutiveSiegesEvent,
			SquSfx.BlitzkriegLuKangUndefendedEvent,
			SquSfx.BlitzkriegHahahaEvent);
	}
}
