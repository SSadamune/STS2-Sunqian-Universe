using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 提词器：每将 3 张牌加入剧本集，抽两张牌。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "teleprompter")]
public sealed class TeleprompterRelic : ScriptRelicTemplate
{
	private const string ScriptAmountKey = "ScriptAmount";

	private bool _isActivating;

	private int _scriptsAdded;

	public override string FlashSfx => "event:/sfx/ui/relic_activate_draw";

	public override RelicRarity Rarity => RelicRarity.Common;

	public override bool ShowCounter => true;

	public override int DisplayAmount
	{
		get
		{
			if (!IsActivating)
			{
				return ScriptsAdded;
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
	public int ScriptsAdded
	{
		get => _scriptsAdded;
		set
		{
			AssertMutable();
			_scriptsAdded = value;
			Status = _scriptsAdded == DynamicVars[ScriptAmountKey].IntValue - 1
				? RelicStatus.Active
				: RelicStatus.Normal;
			InvokeDisplayAmountChanged();
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ScriptAmountKey, 3m),
		new CardsVar(2),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/TeleprompterRelic.png",
		IconOutlinePath: "res://images/relics/TeleprompterRelicOutline.png",
		BigIconPath: "res://images/relics/TeleprompterRelicBig.png");

	public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		if (card.Owner != Owner || oldPileType == ScriptSystem.ScriptCollectionPile
			|| card.Pile?.Type != ScriptSystem.ScriptCollectionPile) return;
		ScriptsAdded++;
		await DrawIfThresholdMet(new BlockingPlayerChoiceContext());
	}

	private async Task DrawIfThresholdMet(PlayerChoiceContext choiceContext)
	{
		int threshold = DynamicVars[ScriptAmountKey].IntValue;
		if (ScriptsAdded < threshold)
		{
			return;
		}

		_ = TaskHelper.RunSafely(DoActivateVisuals());
		int drawCount = ScriptsAdded / threshold * DynamicVars.Cards.IntValue;
		ScriptsAdded %= threshold;
		await CardPileCmd.Draw(choiceContext, drawCount, Owner);
	}

	private async Task DoActivateVisuals()
	{
		IsActivating = true;
		Flash();
		await Cmd.Wait(1f);
		IsActivating = false;
	}
}
