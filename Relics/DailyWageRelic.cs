using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 日结工资：回合结束时若本回合打出过剧本牌，获得金币；不在商人处出售。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "daily_wage")]
public sealed class DailyWageRelic : ScriptRelicTemplate
{
	private bool _playedScriptThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	public override bool IsAllowedInShops => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(4),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/DailyWageRelic.png",
		IconOutlinePath: "res://images/relics/DailyWageRelicOutline.png",
		BigIconPath: "res://images/relics/DailyWageRelicBig.png");

	private bool PlayedScriptThisTurn
	{
		get => _playedScriptThisTurn;
		set
		{
			AssertMutable();
			_playedScriptThisTurn = value;
			Status = value ? RelicStatus.Active : RelicStatus.Normal;
		}
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner
			|| !CombatManager.Instance.IsInProgress
			|| !cardPlay.Card.Tags.Contains(SquCardTags.Script)
			|| PlayedScriptThisTurn)
		{
			return Task.CompletedTask;
		}

		PlayedScriptThisTurn = true;
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner.Creature) || !PlayedScriptThisTurn)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
		PlayedScriptThisTurn = false;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		PlayedScriptThisTurn = false;
		return Task.CompletedTask;
	}
}
