using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 就当我从来没有这些！：记录本局从牌组中移除的家人牌，并在战斗开始时按数量提供能量和敏捷。
/// </summary>
[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "never_had_these")]
public sealed class NeverHadTheseRelic : ModRelicTemplate
{
	private int _removedFamilyCards;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool ShowCounter => RemovedFamilyCards > 0;

	public override int DisplayAmount => RemovedFamilyCards;

	protected override bool IncludeEnergyHoverTip => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

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

	public override Task BeforeCardRemoved(CardModel card)
	{
		if (card.Owner == Owner && card.HasFamily())
		{
			RemovedFamilyCards++;
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
		await PowerCmd.Apply<DexterityPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			RemovedFamilyCards,
			Owner.Creature,
			null);
	}
}
