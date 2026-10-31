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
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Squ.Audio;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本：兼职将军。失效时从打出记录创建带随机附魔的新副本供选择，保留原记录。
/// </summary>
[RegisterPower]
public sealed class ScriptPartTimeGeneralPower : ScriptPowerTemplate
{
	public const string RecordedCardsVarName = "RecordedCards";

	// 每项等概率；同种附魔的不同数值是独立选项。Momentum 沿用人体炼成的 5。
	private static readonly (System.Type Type, int Amount)[] EnchantmentPool =
	[
		(typeof(Vigorous), 6), (typeof(Vigorous), 8),
		(typeof(Swift), 2), (typeof(Swift), 3),
		(typeof(Sown), 1), (typeof(Sown), 2),
		(typeof(Glam), 0), (typeof(Momentum), 5),
		(typeof(Sharp), 4), (typeof(Sharp), 6),
		(typeof(Corrupted), 0),
	];

	private sealed class Data
	{
		public PartTimeGeneralScriptRecord? Record;
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
		IsMutable && GetInternalData<Data>().Record?.RecordedCards.Count > 0
			? Id.Entry + ".smartDescription"
			: Id.Entry + ".smartDescriptionEmpty";

	protected override object InitInternalData() => new Data();

	internal void AttachRecord(PartTimeGeneralScriptRecord record)
	{
		Data data = GetInternalData<Data>();
		data.Record = record;
		((StringVar)DynamicVars[RecordedCardsVarName]).StringValue =
			record.DynamicVars[RecordedCardsVarName].ToString();
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		if (!SuppressScriptEndEffects
			&& oldOwner is { IsAlive: true, Player: { } player, CombatState: { } combatState }
			&& !CombatManager.Instance.IsOverOrEnding)
		{
			SquSfx.Play(SquSfx.PartTimeGeneralBothSidesEvent);
			await ChooseRecordedCard(combatState, player);
		}

		await base.AfterRemoved(oldOwner);
	}

	private async Task ChooseRecordedCard(ICombatState combatState, Player player)
	{
		PartTimeGeneralScriptRecord? record = GetInternalData<Data>().Record;
		if (record is null)
		{
			return;
		}

		List<CardModel> recordedCards = record.RecordedCards
			.Select(CardModel.FromSerializable)
			.ToList();
		if (recordedCards.Count == 0)
		{
			return;
		}

		List<CardModel> options = [];
		foreach (CardModel recorded in recordedCards)
		{
			CardModel option = combatState.CreateCard(
				ModelDb.GetById<CardModel>(recorded.Id),
				player);
			if (recorded.IsUpgraded && option.IsUpgradable)
			{
				option.UpgradeInternal();
				option.FinalizeUpgradeInternal();
			}

			var spec = player.RunState.Rng.CombatCardGeneration.NextItem(EnchantmentPool);
			EnchantmentModel enchantment = ModelDb.GetById<EnchantmentModel>(
				ModelDb.GetId(spec.Type)).ToMutable();
			CardCmd.Enchant(enchantment, option, spec.Amount);
			options.Add(option);
		}

		var prefs = new CardSelectorPrefs(SelectionPrompt, 1)
		{
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
