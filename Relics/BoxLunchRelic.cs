using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
public sealed class BoxLunchRelic : ScriptRelicTemplate, IScriptActivationLimitProvider
{
	public const int UnlocksPerAdditionalSlot = 4;

	public override RelicRarity Rarity => RelicRarity.Starter;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/BoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/BoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/BoxLunchRelicBig.png");

	public int GetAdditionalScriptActivationSlots(Player player, int unlockedScriptCount) =>
		unlockedScriptCount >= UnlocksPerAdditionalSlot ? 1 : 0;

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
