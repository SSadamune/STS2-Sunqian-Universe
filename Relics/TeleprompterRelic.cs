using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 提词器：每消耗三张剧本牌抽一张牌（参考 Joss Paper）。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "teleprompter")]
public sealed class TeleprompterRelic : ScriptRelicTemplate
{
	private const string ScriptAmountKey = "ScriptAmount";

	private bool _isActivating;

	private int _scriptsExhausted;

	private int _etherealCount;

	public override string FlashSfx => "event:/sfx/ui/relic_activate_draw";

	public override RelicRarity Rarity => RelicRarity.Uncommon;

	public override bool ShowCounter => true;

	public override int DisplayAmount
	{
		get
		{
			if (!IsActivating)
			{
				return ScriptsExhausted;
			}

			return DynamicVars[ScriptAmountKey].IntValue;
		}
	}

	private bool IsActivating
	{
		get => _isActivating;
		set
		{
			AssertMutable();
			_isActivating = value;
			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int ScriptsExhausted
	{
		get => _scriptsExhausted;
		set
		{
			AssertMutable();
			_scriptsExhausted = value;
			Status = _scriptsExhausted == DynamicVars[ScriptAmountKey].IntValue - 1
				? RelicStatus.Active
				: RelicStatus.Normal;
			InvokeDisplayAmountChanged();
		}
	}

	private int EtherealCount
	{
		get => _etherealCount;
		set
		{
			AssertMutable();
			_etherealCount = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ScriptAmountKey, 3m),
		new CardsVar(1),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/TeleprompterRelic.png",
		IconOutlinePath: "res://images/relics/TeleprompterRelicOutline.png",
		BigIconPath: "res://images/relics/TeleprompterRelicBig.png");

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card.Owner != Owner || !IsScriptCard(card))
		{
			return;
		}

		if (causedByEthereal)
		{
			EtherealCount++;
			return;
		}

		ScriptsExhausted++;
		await DrawIfThresholdMet(choiceContext);
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner.Creature))
		{
			return;
		}

		ScriptsExhausted += EtherealCount;
		EtherealCount = 0;
		await DrawIfThresholdMet(choiceContext);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		EtherealCount = 0;
		return Task.CompletedTask;
	}

	private async Task DrawIfThresholdMet(PlayerChoiceContext choiceContext)
	{
		int threshold = DynamicVars[ScriptAmountKey].IntValue;
		if (ScriptsExhausted < threshold)
		{
			return;
		}

		TaskHelper.RunSafely(DoActivateVisuals());
		await CardPileCmd.Draw(choiceContext, ScriptsExhausted / threshold, Owner);
		ScriptsExhausted %= threshold;
	}

	private async Task DoActivateVisuals()
	{
		IsActivating = true;
		Flash();
		await Cmd.Wait(1f);
		IsActivating = false;
	}

	private static bool IsScriptCard(CardModel card) => card.Tags.Contains(SquCardTags.Script);
}
