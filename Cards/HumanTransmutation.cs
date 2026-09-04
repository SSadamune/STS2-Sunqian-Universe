using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;
using Squ;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "human_transmutation")]
public sealed class HumanTransmutation : ModCardTemplate
{
	private const int DrawCount = 1;

	private const int MaxExhaustCount = 3;

	private const int SlitherMinCost = 3;

	private readonly record struct EnchantmentSpec(Type EnchantmentType, int Amount);

	private static readonly EnchantmentSpec SlitherSpec = new(typeof(Slither), 0);

	private static readonly EnchantmentSpec[] CommonPool =
	[
		new(typeof(Sharp), 3),
		new(typeof(Swift), 2),
		new(typeof(Vigorous), 6),
		new(typeof(Steady), 0),
		new(typeof(Inky), 0),
	];

	private static readonly EnchantmentSpec[] UncommonPool =
	[
		new(typeof(Swift), 3),
		new(typeof(Sown), 1),
		new(typeof(Inky), 0),
		new(typeof(Glam), 0),
		new(typeof(Corrupted), 0),
	];

	private static readonly EnchantmentSpec[] RarePool =
	[
		new(typeof(Instinct), 0),
		new(typeof(Sown), 2),
		new(typeof(Spiral), 0),
		new(typeof(Sharp), 8),
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(DrawCount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		new HoverTip(
			SquCommonL10n.AnnotationTitle(),
			new LocString("cards", Id.Entry + ".rarityNote")),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/HumanTransmutation.png");

	public HumanTransmutation()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.HumanTransmutationEvent);
		await CardPileCmd.Draw(choiceContext, DrawCount, Owner);

		CardSelectorPrefs exhaustPrefs = new(
			CardSelectorPrefs.ExhaustSelectionPrompt,
			minCount: 0,
			maxCount: MaxExhaustCount);
		List<CardModel> toExhaust = (await CardSelectCmd.FromHand(
			choiceContext,
			Owner,
			exhaustPrefs,
			null,
			this)).ToList();

		foreach (CardModel card in toExhaust)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}

		if (toExhaust.Count == 0)
		{
			return;
		}

		Rng rng = Owner.RunState.Rng.CombatCardGeneration;
		HashSet<Type> usedEnchantmentTypes = [];
		List<CardModel> choices = [];
		foreach (CardModel exhausted in toExhaust)
		{
			CardRarity rarity = MapExhaustedRarity(exhausted.Rarity);
			CardModel? attack = PickRandomAttackForRarity(rarity, rng);
			if (attack is null)
			{
				continue;
			}

			if (IsUpgraded)
			{
				attack.UpgradeInternal();
				attack.FinalizeUpgradeInternal();
			}

			ApplyChoiceEnchantment(attack, rarity, rng, usedEnchantmentTypes);
			choices.Add(attack);
		}

		if (choices.Count == 0)
		{
			return;
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

	private CardModel? PickRandomAttackForRarity(CardRarity rarity, Rng rng)
	{
		List<CardPoolModel> allPools = Owner.UnlockState.CharacterCardPools.ToList();
		if (allPools.Count == 0)
		{
			return null;
		}

		IEnumerable<CardModel> eligibleAttacks = allPools
			.SelectMany(pool => pool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint))
			.Where(card => IsEligibleAttack(card, rarity));

		return CardFactory.GetDistinctForCombat(Owner, eligibleAttacks, 1, rng).FirstOrDefault();
	}

	private static bool IsEligibleAttack(CardModel card, CardRarity rarity) =>
		card.Type == CardType.Attack
		&& card.Rarity == rarity
		&& !CostsStars(card)
		&& !InvolvesOsty(card);

	private static bool CostsStars(CardModel card) =>
		card.HasStarCostX || card.CanonicalStarCost > 0;

	private static bool InvolvesOsty(CardModel card) =>
		card.Tags.Contains(CardTag.OstyAttack)
		|| card.DynamicVars.ContainsKey(OstyDamageVar.defaultName);

	private static void ApplyChoiceEnchantment(
		CardModel card,
		CardRarity rarity,
		Rng rng,
		HashSet<Type> usedTypes)
	{
		EnchantmentSpec spec = TryRollSlither(card, rng, usedTypes)
			?? PickRaritySpec(rarity, rng, usedTypes);
		ApplySpec(card, spec);
		usedTypes.Add(spec.EnchantmentType);
	}

	private static EnchantmentSpec? TryRollSlither(
		CardModel card,
		Rng rng,
		HashSet<Type> usedTypes)
	{
		if (card.EnergyCost.CostsX || card.EnergyCost.Canonical < SlitherMinCost)
		{
			return null;
		}

		if (!rng.NextBool())
		{
			return null;
		}

		if (usedTypes.Contains(typeof(Slither)))
		{
			return null;
		}

		return SlitherSpec;
	}

	private static EnchantmentSpec PickRaritySpec(
		CardRarity rarity,
		Rng rng,
		HashSet<Type> usedTypes)
	{
		EnchantmentSpec[] pool = rarity switch
		{
			CardRarity.Uncommon => UncommonPool,
			CardRarity.Rare => RarePool,
			_ => CommonPool,
		};

		EnchantmentSpec[] unused = pool
			.Where(spec => !usedTypes.Contains(spec.EnchantmentType))
			.ToArray();
		return rng.NextItem(unused.Length > 0 ? unused : pool);
	}

	private static void ApplySpec(CardModel card, EnchantmentSpec spec)
	{
		if (spec.EnchantmentType == typeof(Sharp))
		{
			Apply<Sharp>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Swift))
		{
			Apply<Swift>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Vigorous))
		{
			Apply<Vigorous>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Steady))
		{
			Apply<Steady>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Slither))
		{
			Apply<Slither>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Inky))
		{
			Apply<Inky>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Glam))
		{
			Apply<Glam>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Corrupted))
		{
			Apply<Corrupted>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Instinct))
		{
			Apply<Instinct>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(Sown))
		{
			Apply<Sown>(card, spec.Amount);
			return;
		}

		Apply<Spiral>(card, spec.Amount);
	}

	private static void Apply<T>(CardModel card, int amount)
		where T : EnchantmentModel
	{
		EnchantmentModel enchantment = ModelDb.Enchantment<T>().ToMutable();
		if (enchantment.CanEnchant(card))
		{
			CardCmd.Enchant(enchantment, card, amount);
			return;
		}

		// 涡旋原版只允许基础打击/防御；此处仍按设计贴到稀有攻击上。
		card.EnchantInternal(enchantment, amount);
		enchantment.ModifyCard();
	}

	/// <summary>
	/// 被消耗牌的稀有度映射：先古→稀有，事件→罕见，Common/Uncommon/Rare 保持，其余→普通。
	/// </summary>
	private static CardRarity MapExhaustedRarity(CardRarity rarity) =>
		rarity switch
		{
			CardRarity.Ancient => CardRarity.Rare,
			CardRarity.Event => CardRarity.Uncommon,
			CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare => rarity,
			_ => CardRarity.Common,
		};
}
