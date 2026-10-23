using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using Squ.Cards;

#nullable enable

namespace Squ.Script;

public interface IAfterScriptUnlocked
{
	Task AfterScriptUnlockedAsync(Player player, ScriptRecordCardTemplate record);
}

public interface IAfterScriptCardPlayed
{
	Task AfterScriptCardPlayedAsync(Player player, ScriptRecordCardTemplate record, bool wasNewUnlock);
}

public interface IScriptActivationLimitProvider
{
	int GetAdditionalScriptActivationSlots(Player player, int unlockedScriptCount);
}
