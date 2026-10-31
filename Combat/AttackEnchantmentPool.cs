using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace Squ.Combat;

/// <summary>人体炼成与兼职将军共用的分级攻击牌附魔及应用逻辑。</summary>
internal static class AttackEnchantmentPool
{
	internal readonly record struct EnchantmentSpec(Type EnchantmentType, int Amount);

	internal static readonly EnchantmentSpec SlitherSpec = new(typeof(Slither), 0);

	internal static readonly EnchantmentSpec[] CommonTierPool =
	[
		new(typeof(Vigorous), 6),
		new(typeof(Steady), 0),
		new(typeof(Inky), 0),
	];

	internal static readonly EnchantmentSpec[] UncommonTierPool =
	[
		new(typeof(Momentum), 5),
		new(typeof(Glam), 0),
		new(typeof(Corrupted), 0),
	];

	internal static readonly EnchantmentSpec[] RareTierPool =
	[
		new(typeof(Instinct), 0),
		new(typeof(Spiral), 0),
		new(typeof(TezcatarasEmber), 0),
	];

	internal static readonly EnchantmentSpec[] CommonUniversalPool =
	[
		new(typeof(Sharp), 3),
		new(typeof(Sown), 1),
		new(typeof(Swift), 2),
	];

	internal static readonly EnchantmentSpec[] UncommonUniversalPool =
	[
		new(typeof(Sharp), 5),
		new(typeof(Sown), 1),
		new(typeof(Swift), 3),
	];

	internal static readonly EnchantmentSpec[] RareUniversalPool =
	[
		new(typeof(Sharp), 8),
		new(typeof(Sown), 2),
		new(typeof(Swift), 4),
	];

	internal static EnchantmentSpec[] GetTierPool(int tier) => tier switch
	{
		2 => UncommonTierPool,
		3 => RareTierPool,
		_ => CommonTierPool,
	};

	internal static EnchantmentSpec[] GetUniversalPool(int highestTier) => highestTier switch
	{
		2 => UncommonUniversalPool,
		3 => RareUniversalPool,
		_ => CommonUniversalPool,
	};

	internal static void ApplySpec(CardModel card, EnchantmentSpec spec)
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

		if (spec.EnchantmentType == typeof(Momentum))
		{
			Apply<Momentum>(card, spec.Amount);
			return;
		}

		if (spec.EnchantmentType == typeof(TezcatarasEmber))
		{
			Apply<TezcatarasEmber>(card, spec.Amount);
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

}
