using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Squ.Audio;
using Squ.Cards;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 关帝形态的二选一。选项卡只用于选牌界面，选中后进入对应形态。
/// </summary>
public static class GuanDiFormChoice
{
	private readonly record struct FormCounts(int Normal, int Upgraded)
	{
		public int Total => Normal + Upgraded;

		public FormCounts Add(bool upgraded) =>
			upgraded
				? this with { Upgraded = Upgraded + 1 }
				: this with { Normal = Normal + 1 };
	}

	private static readonly Dictionary<ulong, int> OfferedRoundByPlayer = [];

	private static ICombatState? OfferedCombat;

	private static readonly LocString SelectionScreenPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_GUAN_DI_FORM.selectionScreenPrompt");

	public static async Task OfferAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel? source,
		int sourceEnergySpent,
		bool upgraded,
		bool canSkip)
	{
		if (player.Creature.CombatState is not { } combatState)
		{
			return;
		}

		FormCounts optionCounts = GetFormCounts(player.Creature);
		if (source is not null)
		{
			optionCounts = optionCounts.Add(upgraded);
		}

		CardModel guanYuCivilVer = combatState.CreateCard<GuanYuCivilVer>(player);
		CardModel guanYuMartialVer = combatState.CreateCard<GuanYuMartialVer>(player);
		if (optionCounts.Upgraded > 0)
		{
			guanYuCivilVer.UpgradeInternal();
			guanYuCivilVer.FinalizeUpgradeInternal();
			guanYuMartialVer.UpgradeInternal();
			guanYuMartialVer.FinalizeUpgradeInternal();
		}

		((GuanYuCivilVer)guanYuCivilVer).SetBlockPerEnergy(
			GuanYuCivilVer.BlockPerEnergy * optionCounts.Total);
		((GuanYuMartialVer)guanYuMartialVer).SetFormTotals(
			optionCounts.Normal * GuanYuMartialVer.BaseVigorPerEnergy
				+ optionCounts.Upgraded * GuanYuMartialVer.UpgradedVigorPerEnergy,
			optionCounts.Total * VigorAmplificationPower.BonusStacksPerForm);

		SquSfx.Play(SquSfx.GuanDiFormWhoAreYouEvent);
		CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			[guanYuCivilVer, guanYuMartialVer],
			player,
			canSkip);
		if (chosen is GuanYuCivilVer)
		{
			SquSfx.Play(SquSfx.GuanDiFormCivilSunqianEvent);
			await EnterGuanYuCivilVerAsync(
				choiceContext,
				player,
				source,
				sourceEnergySpent,
				upgraded);
		}
		else if (chosen is GuanYuMartialVer)
		{
			SquSfx.Play(SquSfx.GuanDiFormMartialZhouCangEvent);
			await EnterGuanYuMartialVerAsync(
				choiceContext,
				player,
				source,
				sourceEnergySpent,
				upgraded);
		}
		else if (player.Creature.GetPower<GuanYuMartialVerPower>() is { } martial)
		{
			await RefreshVigorAmplificationAsync(choiceContext, martial);
		}
	}

	public static async Task OfferRechoiceAsync(PowerModel power, ICombatState combatState)
	{
		if (power.Owner.Player is not { } player || !TryBeginOffer(player, combatState))
		{
			return;
		}

		bool upgraded = power switch
		{
			GuanYuCivilVerPower guanYuCivilVer => guanYuCivilVer.FormUpgraded,
			GuanYuMartialVerPower guanYuMartialVer => guanYuMartialVer.FormUpgraded,
			_ => false,
		};
		await OfferAsync(
			new BlockingPlayerChoiceContext(),
			player,
			source: null,
			sourceEnergySpent: 0,
			upgraded,
			canSkip: true);
	}

	private static bool TryBeginOffer(Player player, ICombatState combatState)
	{
		if (!ReferenceEquals(OfferedCombat, combatState))
		{
			OfferedCombat = combatState;
			OfferedRoundByPlayer.Clear();
		}

		if (OfferedRoundByPlayer.TryGetValue(player.NetId, out int round)
			&& round == combatState.RoundNumber)
		{
			return false;
		}

		OfferedRoundByPlayer[player.NetId] = combatState.RoundNumber;
		return true;
	}

	private static async Task EnterGuanYuCivilVerAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel? source,
		int sourceEnergySpent,
		bool upgraded)
	{
		Creature owner = player.Creature;
		FormCounts counts = GetFormCounts(owner);
		if (source is not null)
		{
			counts = counts.Add(upgraded);
		}

		await PowerCmd.Remove<GuanYuMartialVerPower>(owner);

		GuanYuCivilVerPower? power = owner.GetPower<GuanYuCivilVerPower>();
		int targetAmount = GuanYuCivilVer.BlockPerEnergy * counts.Total;
		if (power == null)
		{
			await PowerCmd.Apply<GuanYuCivilVerPower>(
				choiceContext,
				owner,
				targetAmount,
				owner,
				source);
			power = owner.GetPower<GuanYuCivilVerPower>();
		}
		else if (power.Amount != targetAmount)
		{
			await PowerCmd.ModifyAmount(
				choiceContext,
				power,
				targetAmount - power.Amount,
				owner,
				source);
			power = owner.GetPower<GuanYuCivilVerPower>();
		}

		if (power == null)
		{
			return;
		}

		power.SetFormCounts(counts.Normal, counts.Upgraded);
		if (source is not null)
		{
			power.TrackEnergySpent(source, sourceEnergySpent);
		}
		await PowerCmd.Remove<VigorAmplificationPower>(owner);
		await FetchSunqianScriptAsync(player, counts.Upgraded > 0);
	}

	private static async Task EnterGuanYuMartialVerAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel? source,
		int sourceEnergySpent,
		bool upgraded)
	{
		Creature owner = player.Creature;
		FormCounts counts = GetFormCounts(owner);
		if (source is not null)
		{
			counts = counts.Add(upgraded);
		}

		await PowerCmd.Remove<GuanYuCivilVerPower>(owner);

		int targetAmount =
			counts.Normal * GuanYuMartialVer.BaseVigorPerEnergy
			+ counts.Upgraded * GuanYuMartialVer.UpgradedVigorPerEnergy;
		GuanYuMartialVerPower? power = owner.GetPower<GuanYuMartialVerPower>();
		if (power == null)
		{
			await PowerCmd.Apply<GuanYuMartialVerPower>(
				choiceContext,
				owner,
				targetAmount,
				owner,
				source);
			power = owner.GetPower<GuanYuMartialVerPower>();
		}
		else if (power.Amount != targetAmount)
		{
			await PowerCmd.ModifyAmount(
				choiceContext,
				power,
				targetAmount - power.Amount,
				owner,
				source);
			power = owner.GetPower<GuanYuMartialVerPower>();
		}

		if (power == null)
		{
			return;
		}

		power.SetFormCounts(counts.Normal, counts.Upgraded);
		if (source is not null)
		{
			power.TrackEnergySpent(source, sourceEnergySpent);
		}
		await SetVigorAmplificationAsync(
			choiceContext,
			owner,
			counts.Total * VigorAmplificationPower.BonusStacksPerForm,
			source);
	}

	public static Task RefreshVigorAmplificationAsync(
		PlayerChoiceContext choiceContext,
		GuanYuMartialVerPower power) =>
		SetVigorAmplificationAsync(
			choiceContext,
			power.Owner,
			power.FormCount * VigorAmplificationPower.BonusStacksPerForm,
			source: null);

	private static async Task SetVigorAmplificationAsync(
		PlayerChoiceContext choiceContext,
		Creature owner,
		int targetAmount,
		CardModel? source)
	{
		VigorAmplificationPower? current =
			owner.GetPower<VigorAmplificationPower>();
		if (targetAmount <= 0)
		{
			await PowerCmd.Remove(current);
			return;
		}

		if (current == null)
		{
			await PowerCmd.Apply<VigorAmplificationPower>(
				choiceContext,
				owner,
				targetAmount,
				owner,
				source);
			return;
		}

		if (current.Amount != targetAmount)
		{
			await PowerCmd.ModifyAmount(
				choiceContext,
				current,
				targetAmount - current.Amount,
				owner,
				source);
		}
	}

	private static FormCounts GetFormCounts(Creature owner)
	{
		if (owner.GetPower<GuanYuCivilVerPower>() is { } civil)
		{
			return new FormCounts(civil.NormalFormCount, civil.UpgradedFormCount);
		}

		if (owner.GetPower<GuanYuMartialVerPower>() is { } martial)
		{
			return new FormCounts(martial.NormalFormCount, martial.UpgradedFormCount);
		}

		return default;
	}

	private static async Task FetchSunqianScriptAsync(Player player, bool freeUntilPlayed)
	{
		CardModel? script =
			FindScriptInPile(player, PileType.Draw)
			?? FindScriptInPile(player, PileType.Discard)
			?? FindScriptInPile(player, PileType.Exhaust);

		if (script is null)
		{
			CardModel? scriptInHand = PileType.Hand
				.GetPile(player)
				.Cards
				.FirstOrDefault(card => card is SunqianScript);
			if (scriptInHand is not null)
			{
				if (freeUntilPlayed)
				{
					scriptInHand.EnergyCost.SetUntilPlayed(0);
				}

				return;
			}

			if (player.Creature.CombatState is not { } combatState)
			{
				return;
			}

			script = combatState.CreateCard<SunqianScript>(player);
			if (freeUntilPlayed)
			{
				script.EnergyCost.SetUntilPlayed(0);
			}

			await CardPileCmd.AddGeneratedCardToCombat(script, PileType.Hand, player);
			return;
		}

		await CardPileCmd.Add(script, PileType.Hand);
		if (freeUntilPlayed)
		{
			script.EnergyCost.SetUntilPlayed(0);
		}
	}

	private static CardModel? FindScriptInPile(Player player, PileType pileType)
	{
		List<CardModel> scripts = pileType
			.GetPile(player)
			.Cards
			.Where(card => card is SunqianScript)
			.ToList();
		return scripts.Count > 0
			? player.RunState.Rng.CombatCardGeneration.NextItem(scripts)
			: null;
	}

	[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready))]
	private static class SelectionScreenTitlePatch
	{
		private static void Postfix(NChooseACardSelectionScreen __instance)
		{
			if (AccessTools.Field(typeof(NChooseACardSelectionScreen), "_cards")
					.GetValue(__instance) is not IReadOnlyList<CardModel> cards
				|| cards.Count != 2
				|| !cards.Any(card => card is GuanYuCivilVer)
				|| !cards.Any(card => card is GuanYuMartialVer)
				|| AccessTools.Field(typeof(NChooseACardSelectionScreen), "_banner")
					.GetValue(__instance) is not NCommonBanner banner)
			{
				return;
			}

			banner.label.SetTextAutoSize(SelectionScreenPrompt.GetRawText());
		}
	}
}
