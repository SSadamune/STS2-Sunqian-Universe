using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 《孟德新书》：敌人正面状态种类减伤；自身负面状态种类增伤（每种 10%，乘算叠加）。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "mengde_new_book")]
public sealed class MengdeNewBookRelic : ModRelicTemplate
{
	public const decimal PercentPerPowerType = 0.10m;

	public override RelicRarity Rarity => RelicRarity.Rare;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/MengdeNewBookRelic.png",
		IconOutlinePath: "res://images/relics/MengdeNewBookRelicOutline.png",
		BigIconPath: "res://images/relics/MengdeNewBookRelicBig.png");

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (dealer is null || !props.IsPoweredAttack())
		{
			return 1m;
		}

		if (target == Owner.Creature && dealer.IsMonster)
		{
			int buffTypes = CountDistinctPowerTypes(dealer.Powers, PowerType.Buff);
			if (buffTypes > 0)
			{
				// 乘算：两种 buff → 0.9 × 0.9 = 0.81（约减伤 19%）
				return Raise(1m - PercentPerPowerType, buffTypes);
			}
		}

		if (dealer == Owner.Creature)
		{
			int debuffTypes = CountDistinctPowerTypes(Owner.Creature.Powers, PowerType.Debuff);
			if (debuffTypes > 0)
			{
				// 乘算：两种 debuff → 1.1 × 1.1 = 1.21（约增伤 21%）
				return Raise(1m + PercentPerPowerType, debuffTypes);
			}
		}

		return 1m;
	}

	public override Task AfterModifyingDamageAmount(CardModel? cardSource)
	{
		Flash();
		return Task.CompletedTask;
	}

	private static decimal Raise(decimal factor, int stacks)
	{
		decimal result = 1m;
		for (int i = 0; i < stacks; i++)
		{
			result *= factor;
		}

		return result;
	}

	private static int CountDistinctPowerTypes(IEnumerable<PowerModel> powers, PowerType type) =>
		powers.Where(p => p.Type == type && p.Amount > 0m)
			.Select(p => p.Id)
			.Distinct()
			.Count();
}
