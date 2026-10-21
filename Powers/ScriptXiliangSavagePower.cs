using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 西凉野人剧本：持有者的牌会永久保留本次活力带来的数值加成。
/// 攻击伤害在攻击结束后立刻写回；格挡、灼烧等在本次打出中才读取的加成
/// 延迟到整张牌结算后写回，避免当前这次打出被重复计算。
/// 「上将军」继承给嵌套攻击牌的活力加成同样由实际获得该加成的子牌保留。
/// </summary>
[RegisterPower]
public sealed class ScriptXiliangSavagePower : ScriptPowerTemplate
{
	private const string BlockVarName = "Block";
	private static readonly string BurningVarName = nameof(BurningPower);

	private sealed class Data
	{
		public Dictionary<AttackCommand, PendingAttack> PendingAttacks { get; } = [];

		public Dictionary<CardModel, PendingCardBonuses> PendingCardBonuses { get; } = [];

		public Dictionary<CardModel, ResourceSnapshot> ResourceSnapshots { get; } = [];
	}

	private sealed class ResourceSnapshot
	{
		public required decimal VigorBonus { get; init; }

		public required decimal DrunkenMightBonus { get; init; }

		public bool HasSeenPoweredAttack { get; set; }

		public HashSet<string> RetainedDamageVars { get; } = [];

		public bool HasRetainedBurning { get; set; }
	}

	private sealed class PendingAttack
	{
		public required CardModel Card { get; init; }

		public required string DamageVarName { get; init; }

		public required decimal RetainedBonus { get; init; }

		public required bool RetainBurning { get; init; }
	}

	private sealed class PendingCardBonuses
	{
		public decimal Block;

		public decimal Burning;
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		NewsanguoDrunkenMightEffect.HoverTips();

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptXiliangSavagePower.png",
		BigIconPath: "res://images/powers/ScriptXiliangSavagePowerBig.png");

	protected override object InitInternalData() => new Data();

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == Owner
			&& cardPlay.Card.Type == CardType.Attack
			&& cardPlay.IsFirstInSeries)
		{
			GetInternalData<Data>().ResourceSnapshots[cardPlay.Card] = new ResourceSnapshot
			{
				VigorBonus = SquVigorSnapshot.GetEffectiveAmount(Owner, cardPlay.Card),
				DrunkenMightBonus = NewsanguoDrunkenMightEffect.GetBonus(Owner, cardPlay.Card),
			};
		}

		return Task.CompletedTask;
	}

	public override Task BeforeAttack(AttackCommand command)
	{
		if (Owner.IsDead || !TryGetQualifyingAttackCard(command, out CardModel? card))
		{
			return Task.CompletedTask;
		}

		if (!command.DamageProps.IsPoweredAttack())
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		if (!data.ResourceSnapshots.TryGetValue(card, out ResourceSnapshot? snapshot))
		{
			snapshot = new ResourceSnapshot
			{
				VigorBonus = SquVigorSnapshot.GetEffectiveAmount(Owner, card),
				DrunkenMightBonus = NewsanguoDrunkenMightEffect.GetBonus(Owner, card),
			};
			data.ResourceSnapshots[card] = snapshot;
		}

		decimal inheritedVigorBonus = 0m;
		if (SupremeGeneralKeywordSystem.TryGetInheritedVigorBonus(
			card,
			command.CardPlay,
			out decimal inheritedVigor))
		{
			inheritedVigorBonus = inheritedVigor;
		}

		decimal carriedVigorBonus = snapshot.VigorBonus + inheritedVigorBonus;
		decimal drunkenMightBonus = snapshot.DrunkenMightBonus;
		drunkenMightBonus += SupremeGeneralKeywordSystem.GetInheritedDrunkenMightBonus(
			card,
			command.CardPlay);
		string? damageVarName = CardValueRetain.ResolveDamageVarName(
			card,
			command,
			carriedVigorBonus);
		if (damageVarName is null)
		{
			return Task.CompletedTask;
		}

		bool rawDamageCarriesSnapshot = CardValueRetain.TryGetRawAttackDamage(
			command,
			out decimal rawDamage)
			&& card.DynamicVars[damageVarName].BaseValue == rawDamage - carriedVigorBonus;
		decimal retainedVigorBonus = !snapshot.HasSeenPoweredAttack
			|| rawDamageCarriesSnapshot
			|| SquVigorSnapshot.GetAmount(Owner) > 0
				? carriedVigorBonus
				: inheritedVigorBonus;
		snapshot.HasSeenPoweredAttack = true;
		decimal retainedBonus = retainedVigorBonus + drunkenMightBonus;

		if (retainedBonus <= 0m || !snapshot.RetainedDamageVars.Add(damageVarName))
		{
			return Task.CompletedTask;
		}

		bool retainBurning = card.DynamicVars.ContainsKey(BurningVarName)
			&& !snapshot.HasRetainedBurning;
		snapshot.HasRetainedBurning |= retainBurning;

		data.PendingAttacks[command] = new PendingAttack
		{
			Card = card,
			DamageVarName = damageVarName,
			RetainedBonus = retainedBonus,
			RetainBurning = retainBurning,
		};

		return Task.CompletedTask;
	}

	public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		Data data = GetInternalData<Data>();
		if (!data.PendingAttacks.Remove(command, out PendingAttack? pending))
		{
			return Task.CompletedTask;
		}

		decimal retainedBonus = pending.RetainedBonus;
		if (retainedBonus <= 0m)
		{
			return Task.CompletedTask;
		}

		bool retained = CardValueRetain.TryAddBaseValue(
			pending.Card,
			pending.DamageVarName,
			retainedBonus);
		if (pending.RetainBurning)
		{
			QueueCardBonus(pending.Card, burning: retainedBonus);
			retained = true;
		}

		if (retained)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (!NewsanguoDrunkenMightEffect.IsEffectResource(power, Owner)
			|| amount >= 0m
			|| cardSource is null
			|| cardSource.Owner.Creature != Owner
			|| !cardSource.DynamicVars.ContainsKey(BlockVarName))
		{
			return Task.CompletedTask;
		}

		decimal retainedBlock = power is VigorPower
			? SquVigorSnapshot.ApplyBonusPercentage(Owner, cardSource, -amount)
			: -amount * NewsanguoDrunkenMightEffect.GetAttackScale(Owner, cardSource);
		QueueCardBonus(cardSource, block: retainedBlock);
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.IsLastInSeries)
		{
			GetInternalData<Data>().ResourceSnapshots.Remove(cardPlay.Card);
		}

		if (cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.PlayIndex != cardPlay.PlayCount - 1
			|| !GetInternalData<Data>().PendingCardBonuses.Remove(
				cardPlay.Card,
				out PendingCardBonuses? bonuses))
		{
			return Task.CompletedTask;
		}

		bool retained = false;
		if (bonuses.Block > 0m)
		{
			retained |= CardValueRetain.TryAddBaseValue(
				cardPlay.Card,
				BlockVarName,
				bonuses.Block);
		}

		if (bonuses.Burning > 0m)
		{
			retained |= CardValueRetain.TryAddBaseValue(
				cardPlay.Card,
				BurningVarName,
				bonuses.Burning);
		}

		if (retained)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private void QueueCardBonus(
		CardModel card,
		decimal block = 0m,
		decimal burning = 0m)
	{
		Data data = GetInternalData<Data>();
		if (!data.PendingCardBonuses.TryGetValue(card, out PendingCardBonuses? bonuses))
		{
			bonuses = new PendingCardBonuses();
			data.PendingCardBonuses.Add(card, bonuses);
		}

		bonuses.Block += block;
		bonuses.Burning += burning;
	}

	private bool TryGetQualifyingAttackCard(AttackCommand command, out CardModel card)
	{
		card = null!;
		if (command.Attacker != Owner)
		{
			return false;
		}

		if (command.ModelSource is not CardModel cardSource || cardSource.Type != CardType.Attack)
		{
			return false;
		}

		if (cardSource.Owner.Creature != Owner)
		{
			return false;
		}

		card = cardSource;
		return true;
	}
}
