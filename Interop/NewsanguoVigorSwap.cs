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
/// 角色转换联动：《酒》《换大盏》《沛国佳酿》给予资源时，只有新三国角色
/// 将活力转换为酒力；其余角色仍获得活力。运行时只走
/// <see cref="NewsanguoPublicApiInterop"/>。
/// </summary>
public static class NewsanguoVigorSwap
{
	public static bool IsModReady => NewsanguoPublicApiInterop.IsReady;

	/// <summary>角色转换模式：该目标获得的活力是否应改为酒力。</summary>
	public static bool ShouldConvertGrantedVigor(Creature? target) =>
		ShouldConvertGrantedVigor(target?.Player);

	/// <summary>角色转换模式：该玩家获得的活力是否应改为酒力。</summary>
	public static bool ShouldConvertGrantedVigor(Player? player) =>
		IsModReady && NewsanguoPublicApiInterop.IsNewsanguoCharacter(player?.Character);

	/// <summary>兼容旧调用；新代码应使用 <see cref="ShouldConvertGrantedVigor(Creature?)"/>。</summary>
	public static bool AffectsNewsanguo(Creature? target) =>
		ShouldConvertGrantedVigor(target);

	/// <summary>兼容旧调用；新代码应使用 <see cref="ShouldConvertGrantedVigor(Player?)"/>。</summary>
	public static bool AffectsNewsanguo(Player? player) =>
		ShouldConvertGrantedVigor(player);

	public static IEnumerable<IHoverTip> LibraryHoverTips()
	{
		bool inCombat = IsCombatInProgress();
		bool willShow = PresentationMode == JointModPresentationMode.HoverOnly;
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
		if (ShowsInlineKeywords() && TryCreateDrunkenMightHoverTip() is { } drunkenMight)
		{
			yield return drunkenMight;
			yield break;
		}

		yield return HoverTipFactory.FromPower<VigorPower>();
	}

	public static void AddCombatCondition(LocString description, CardModel card) =>
		description.Add(
			"NewsanguoPowerName",
			CombatPowerName(MutableOwner(card)));

	public static void AddCombatNote(LocString description, PotionModel potion) =>
		description.Add(
			"NewsanguoCombatNote",
			CombatNoteOrEmpty(MutableOwner(potion), SquCommonL10n.NewsanguoCombatNote()));

	public static void AddCombatPowerCondition(LocString description, PowerModel power) =>
		description.Add(
			"NewsanguoPowerName",
			CombatPowerName(MutableOwner(power)?.Player));

	public static Task ApplyVigorOrDrunkenMight(
		PlayerChoiceContext choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		bool swap = ShouldConvertGrantedVigor(target);
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
		bool appended = ShowsInlineKeywords();
		NewsanguoInteropDiagnostics.LogCombatNote(owner, appended);
		if (!appended)
		{
			return string.Empty;
		}

		return "\n[purple]" + note.GetFormattedText() + "[/purple]";
	}

	private static string CombatPowerName(Creature? owner) =>
		CombatPowerName(owner?.Player);

	private static string CombatPowerName(Player? owner) =>
		(ShowsInlineKeywords()
			? SquCommonL10n.NewsanguoDrunkenMightInlineText()
			: SquCommonL10n.NewsanguoVigorInlineText()).GetFormattedText();

	private static JointModPresentationMode PresentationMode =>
		JointModPresentation.Resolve(
			IsModReady,
			NewsanguoPublicApiInterop.IsNewsanguoCharacter);

	private static bool ShowsInlineKeywords() =>
		PresentationMode == JointModPresentationMode.InlineKeywords;

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
