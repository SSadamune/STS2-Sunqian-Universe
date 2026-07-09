using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Potions;

/// <summary>
/// 天意小纸条：从 3 张升级过的、带消耗的先古卡中选择 1 张加入手牌。
/// 不走 <see cref="MegaCrit.Sts2.Core.Factories.CardFactory.GetDistinctForCombat"/>，
/// 因其 <c>FilterForCombat</c> 会排除所有 Ancient。
/// </summary>
[RegisterPotion(typeof(SunqianPotionPool), StableEntryStem = "heavenly_note")]
public sealed class HeavenlyNotePotion : ModPotionTemplate
{
	private const int ChoiceCount = 3;

	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(ChoiceCount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override PotionAssetProfile AssetProfile => new(
		ImagePath: "res://images/potions/HeavenlyNote.png",
		OutlinePath: "res://images/potions/HeavenlyNoteOutline.png");

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		ICombatState combatState = Owner.Creature.CombatState
			?? throw new InvalidOperationException("HeavenlyNotePotion requires an active combat.");

		List<CardModel> pool = ModelDb.AllCardPools
			.SelectMany(cardPool => cardPool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint))
			.Where(IsEligibleAncientExhaustCard)
			.Distinct()
			.ToList();

		if (pool.Count == 0)
		{
			return;
		}

		Rng rng = Owner.RunState.Rng.CombatCardGeneration;
		List<CardModel> choices = pool
			.TakeRandom(Math.Min(ChoiceCount, pool.Count), rng)
			.Select(canonical => combatState.CreateCard(canonical, Owner))
			.ToList();

		foreach (CardModel choice in choices)
		{
			choice.UpgradeInternal();
			choice.FinalizeUpgradeInternal();
		}

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			choices,
			Owner,
			canSkip: true);

		if (selected is not null)
		{
			await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
		}
	}

	private static bool IsEligibleAncientExhaustCard(CardModel card) =>
		card.Rarity == CardRarity.Ancient
		&& card.Keywords.Contains(CardKeyword.Exhaust);
}
