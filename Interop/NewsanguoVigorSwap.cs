using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// 《酒》《换大盏》《沛国佳酿》：新三国角色获得酒力，其余角色获得活力。
/// 运行时只走 <see cref="NewsanguoPublicApiInterop"/>。
/// </summary>
public static class NewsanguoVigorSwap
{
	public static bool IsModReady => NewsanguoPublicApiInterop.IsReady;

	public static bool AffectsNewsanguo(Creature? target) =>
		AffectsNewsanguo(target?.Player);

	public static bool AffectsNewsanguo(Player? player) =>
		IsModReady && NewsanguoPublicApiInterop.IsNewsanguoCharacter(player?.Character);

	public static IEnumerable<IHoverTip> LibraryHoverTips()
	{
		bool inCombat = IsCombatInProgress();
		bool willShow = IsModReady && !inCombat;
		NewsanguoInteropDiagnostics.LogLibraryHover(inCombat, willShow);
		if (!willShow)
		{
			yield break;
		}

		yield return new HoverTip(
			SquCommonL10n.NewsanguoVigorSwapHoverTitle(),
			SquCommonL10n.NewsanguoVigorSwapLibraryHover());
	}

	public static IEnumerable<IHoverTip> GrantedPowerHoverTips(CardModel card) =>
		GrantedPowerHoverTips(MutableOwner(card));

	public static IEnumerable<IHoverTip> GrantedPowerHoverTips(PotionModel potion) =>
		GrantedPowerHoverTips(MutableOwner(potion));

	public static IEnumerable<IHoverTip> GrantedPowerHoverTips(PowerModel power) =>
		GrantedPowerHoverTips(MutableOwner(power)?.Player);

	private static IEnumerable<IHoverTip> GrantedPowerHoverTips(Player? owner)
	{
		if (ShowsPurpleNote(owner) && TryCreateDrunkenMightHoverTip() is { } drunkenMight)
		{
			yield return drunkenMight;
			yield break;
		}

		yield return HoverTipFactory.FromPower<VigorPower>();
	}

	public static void AddCombatNote(LocString description, CardModel card) =>
		description.Add(
			"NewsanguoCombatNote",
			CombatNoteOrEmpty(MutableOwner(card), SquCommonL10n.NewsanguoCombatNote()));

	public static void AddCombatNote(LocString description, PotionModel potion) =>
		description.Add(
			"NewsanguoCombatNote",
			CombatNoteOrEmpty(MutableOwner(potion), SquCommonL10n.NewsanguoCombatNote()));

	public static void AddCombatPowerNote(LocString description, PowerModel power) =>
		description.Add(
			"NewsanguoCombatNote",
			CombatNoteOrEmpty(MutableOwner(power), SquCommonL10n.NewsanguoCombatPowerNote()));

	public static Task ApplyVigorOrDrunkenMight(
		PlayerChoiceContext choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		bool swap = AffectsNewsanguo(target);
		NewsanguoInteropDiagnostics.LogApply(target, swap);
		if (swap)
		{
			return NewsanguoPublicApiInterop.ApplyDrunkenMight(
				choiceContext,
				target,
				amount,
				applier,
				cardSource,
				silent);
		}

		return PowerCmd.Apply<VigorPower>(
			choiceContext,
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	private static Player? MutableOwner(CardModel card) =>
		card.IsMutable ? card.Owner : null;

	private static Player? MutableOwner(PotionModel potion) =>
		potion.IsMutable ? potion.Owner : null;

	private static Creature? MutableOwner(PowerModel power) =>
		power.IsMutable ? power.Owner : null;

	private static string CombatNoteOrEmpty(Creature? owner, LocString note) =>
		CombatNoteOrEmpty(owner?.Player, note);

	private static string CombatNoteOrEmpty(Player? owner, LocString note)
	{
		bool appended = ShowsPurpleNote(owner);
		NewsanguoInteropDiagnostics.LogCombatNote(owner, appended);
		if (!appended)
		{
			return string.Empty;
		}

		return "\n[purple]" + note.GetFormattedText() + "[/purple]";
	}

	private static bool ShowsPurpleNote(Player? owner) =>
		IsCombatInProgress() && AffectsNewsanguo(owner);

	private static IHoverTip? TryCreateDrunkenMightHoverTip()
	{
		if (!IsModReady)
		{
			return null;
		}

		return NewsanguoPublicApiInterop.CreateDrunkenMightHoverTip();
	}

	private static bool IsCombatInProgress() =>
		CombatManager.Instance is { IsInProgress: true };
}
