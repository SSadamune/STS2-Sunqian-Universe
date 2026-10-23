using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Cards;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "box_lunch")]
public sealed class BoxLunchRelic : ScriptRelicTemplate, IAfterScriptUnlocked
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override bool IncludeEnergyHoverTip => true;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/BoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/BoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/BoxLunchRelicBig.png");

	public async Task AfterScriptUnlockedAsync(Player player, ScriptRecordCardTemplate record)
	{
		Flash();
		await PlayerCmd.GainEnergy(1m, player);
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Creature.Side
			|| !participants.Contains(Owner.Creature)
			|| ScriptSystem.ScriptCollectionPile.GetPile(Owner).Cards.Count == 0)
		{
			return;
		}

		Status = RelicStatus.Active;
		Flash();
		await ScriptSystem.ChooseActiveScriptsAsync(
			new BlockingPlayerChoiceContext(),
			Owner,
			cancelable: true);
		Status = RelicStatus.Normal;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}
}
