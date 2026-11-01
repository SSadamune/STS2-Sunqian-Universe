using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Audio;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 就当我从来没有这些！：记录本局从牌组中移除的家人牌，并在战斗开始时按数量
/// 提供能量、触发这些牌被消耗时的效果。
/// </summary>
[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "never_had_these")]
public sealed class NeverHadTheseRelic : ModRelicTemplate
{
	private int _removedFamilyCards;
	private int _removedCutePets;
	private int _removedBelovedPartners;
	private int _removedNextOfKin;
	private int _removedUpgradedCutePets;
	private int _removedUpgradedBelovedPartners;
	private int _removedUpgradedNextOfKin;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool ShowCounter => RemovedFamilyCards > 0;

	public override int DisplayAmount => RemovedFamilyCards;

	protected override bool IncludeEnergyHoverTip => true;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/NeverHadTheseRelic.png",
		IconOutlinePath: "res://images/relics/NeverHadTheseRelicOutline.png",
		BigIconPath: "res://images/relics/NeverHadTheseRelicBig.png");

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedFamilyCards
	{
		get => _removedFamilyCards;
		set
		{
			AssertMutable();
			_removedFamilyCards = value;
			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedCutePets
	{
		get => _removedCutePets;
		set
		{
			AssertMutable();
			_removedCutePets = value;
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedBelovedPartners
	{
		get => _removedBelovedPartners;
		set
		{
			AssertMutable();
			_removedBelovedPartners = value;
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedNextOfKin
	{
		get => _removedNextOfKin;
		set
		{
			AssertMutable();
			_removedNextOfKin = value;
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedUpgradedCutePets
	{
		get => _removedUpgradedCutePets;
		set
		{
			AssertMutable();
			_removedUpgradedCutePets = value;
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedUpgradedBelovedPartners
	{
		get => _removedUpgradedBelovedPartners;
		set
		{
			AssertMutable();
			_removedUpgradedBelovedPartners = value;
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int RemovedUpgradedNextOfKin
	{
		get => _removedUpgradedNextOfKin;
		set
		{
			AssertMutable();
			_removedUpgradedNextOfKin = value;
		}
	}

	public override Task BeforeCardRemoved(CardModel card)
	{
		if (card.Owner == Owner && card.HasFamily())
		{
			if (card is FamilyCardTemplate familyCard)
			{
				familyCard.PlayExhaustSfx();
			}

			RemovedFamilyCards++;
			switch (card)
			{
			case CutePet:
				RemovedCutePets++;
				if (card.IsUpgraded)
				{
					RemovedUpgradedCutePets++;
				}
				break;
			case BelovedPartner:
				RemovedBelovedPartners++;
				if (card.IsUpgraded)
				{
					RemovedUpgradedBelovedPartners++;
				}
				break;
			case NextOfKin:
				RemovedNextOfKin++;
				if (card.IsUpgraded)
				{
					RemovedUpgradedNextOfKin++;
				}
				break;
			}
			Flash();
		}

		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (RemovedFamilyCards <= 0
			|| !participants.Contains(Owner.Creature)
			|| Owner.PlayerCombatState?.TurnNumber > 1)
		{
			return;
		}

		Flash();
		SquSfx.Play(SquSfx.NeverHadTheseRelicTriggerEvent);
		await PlayerCmd.GainEnergy(RemovedFamilyCards, Owner);

		var choiceContext = new ThrowingPlayerChoiceContext();
		for (int index = 0; index < RemovedCutePets; index++)
		{
			await CutePet.ResolveExhaustEffect(
				choiceContext,
				Owner,
				index < RemovedUpgradedCutePets
					? CutePet.UpgradedSummonAmount
					: CutePet.SummonAmount,
				this);
		}

		for (int index = 0; index < RemovedBelovedPartners; index++)
		{
			await BelovedPartner.ResolveExhaustEffect(
				choiceContext,
				Owner,
				index < RemovedUpgradedBelovedPartners
					? BelovedPartner.UpgradedRegenAmount
					: BelovedPartner.RegenAmount,
				BelovedPartner.TinderAmount,
				null);
		}

		for (int index = 0; index < RemovedNextOfKin; index++)
		{
			await NextOfKin.ResolveExhaustEffect(
				choiceContext,
				Owner,
				index < RemovedUpgradedNextOfKin
					? NextOfKin.UpgradedDrawAmount
					: NextOfKin.DrawAmount);
		}
	}
}
