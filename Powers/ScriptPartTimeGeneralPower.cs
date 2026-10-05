using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本：兼职将军。记录卡牌打出时生成的 Strike；剧本失效后让持有者从记录中
/// 选择一种，并将同升级状态的新副本加入手牌。
/// </summary>
[RegisterPower]
public sealed class ScriptPartTimeGeneralPower : ScriptPowerTemplate
{
	public const string RecordedCardsVarName = "RecordedCards";

	private sealed record RecordedCard(ModelId Id, bool Upgraded);

	private sealed class Data
	{
		public List<RecordedCard> PlayedCards { get; } = [];
	}

	private static readonly LocString SelectionPrompt =
		new("powers", "SUNQIAN_UNIVERSE_POWER_SCRIPT_PART_TIME_GENERAL_POWER.selectionScreenPrompt");

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptPartTimeGeneralPower.png",
		BigIconPath: "res://images/powers/ScriptPartTimeGeneralPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(RecordedCardsVarName, string.Empty),
	];

	protected override string SmartDescriptionLocKey =>
		IsMutable && GetInternalData<Data>().PlayedCards.Count > 0
			? Id.Entry + ".smartDescription"
			: Id.Entry + ".smartDescriptionEmpty";

	protected override object InitInternalData() => new Data();

	internal void RecordPlayedCards(IEnumerable<CardModel> cards)
	{
		List<CardModel> cardList = cards.ToList();
		Data data = GetInternalData<Data>();
		foreach (CardModel card in cardList)
		{
			data.PlayedCards.Add(new RecordedCard(card.Id, card.IsUpgraded));
		}

		string separator = new LocString(
			"powers",
			Id.Entry + ".cardSeparator").GetFormattedText() ?? ", ";
		((StringVar)DynamicVars[RecordedCardsVarName]).StringValue = string.Join(
			separator,
			cardList.Select(card => $"[gold]{card.Title}[/gold]"));
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		if (oldOwner is { IsAlive: true, Player: { } player, CombatState: { } combatState }
			&& !CombatManager.Instance.IsOverOrEnding)
		{
			await ChooseRecordedCard(combatState, player);
		}

		await base.AfterRemoved(oldOwner);
	}

	private async Task ChooseRecordedCard(ICombatState combatState, Player player)
	{
		List<RecordedCard> recordedCards = GetInternalData<Data>().PlayedCards
			.Distinct()
			.ToList();
		if (recordedCards.Count == 0)
		{
			return;
		}

		List<CardModel> options = [];
		foreach (RecordedCard recorded in recordedCards)
		{
			CardModel option = combatState.CreateCard(
				ModelDb.GetById<CardModel>(recorded.Id),
				player);
			if (recorded.Upgraded && option.IsUpgradable)
			{
				option.UpgradeInternal();
				option.FinalizeUpgradeInternal();
			}

			options.Add(option);
		}

		var prefs = new CardSelectorPrefs(SelectionPrompt, 1)
		{
			RequireManualConfirmation = true,
			PretendCardsCanBePlayed = true,
		};
		CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
			new BlockingPlayerChoiceContext(),
			options,
			player,
			prefs)).FirstOrDefault();
		selected ??= options[0];

		await CardPileCmd.AddGeneratedCardToCombat(
			selected,
			PileType.Hand,
			player);

		foreach (CardModel option in options)
		{
			if (!ReferenceEquals(option, selected))
			{
				option.RemoveFromState();
			}
		}
	}
}
