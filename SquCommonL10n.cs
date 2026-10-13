using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;

namespace Squ;

/// <summary>
/// 跨卡牌/能力复用的 mod 自有本地化表（<see cref="SquMod.CommonL10nStem"/>）。
/// </summary>
public static class SquCommonL10n
{
	public const string AnnotationTitleKey = "SUNQIAN_UNIVERSE_COMMON.ANNOTATION.title";
	public const string NewsanguoVigorSwapHoverTitleKey =
		"SUNQIAN_UNIVERSE_COMMON.NEWSANGUO_VIGOR_SWAP.hoverTitle";
	public const string NewsanguoVigorSwapLibraryHoverKey =
		"SUNQIAN_UNIVERSE_COMMON.NEWSANGUO_VIGOR_SWAP.libraryHover";
	public const string NewsanguoCombatNoteKey =
		"SUNQIAN_UNIVERSE_COMMON.NEWSANGUO_VIGOR_SWAP.combatNote";
	public const string NewsanguoCombatPowerNoteKey =
		"SUNQIAN_UNIVERSE_COMMON.NEWSANGUO_VIGOR_SWAP.combatPowerNote";
	public const string ScrySelectionPromptWithSourceKey =
		"SUNQIAN_UNIVERSE_COMMON.SCRY.selectionPromptWithSource";
	public const string NeutralCardAddedAnnotationKey =
		"SUNQIAN_UNIVERSE_COMMON.NEUTRAL_CARD_ADDED.annotation";
	public const string NeutralPotionModifiedAnnotationKey =
		"SUNQIAN_UNIVERSE_COMMON.NEUTRAL_POTION_MODIFIED.annotation";
	public const string PlayRateTitleKey =
		"SUNQIAN_UNIVERSE_COMMON.PLAY_RATE.title";
	public const string PlayRateAnnotationKey =
		"SUNQIAN_UNIVERSE_COMMON.PLAY_RATE.annotation";

	public static string Table =>
		RitsuLibFramework.GetI18NLocTableId(SquMod.ModId, SquMod.CommonL10nStem);

	public static LocString AnnotationTitle() => new(Table, AnnotationTitleKey);

	public static LocString NewsanguoVigorSwapHoverTitle() =>
		new(Table, NewsanguoVigorSwapHoverTitleKey);

	public static LocString NewsanguoVigorSwapLibraryHover() =>
		new(Table, NewsanguoVigorSwapLibraryHoverKey);

	public static LocString NewsanguoCombatNote() =>
		new(Table, NewsanguoCombatNoteKey);

	public static LocString NewsanguoCombatPowerNote() =>
		new(Table, NewsanguoCombatPowerNoteKey);

	public static LocString ScrySelectionPromptWithSource() =>
		new(Table, ScrySelectionPromptWithSourceKey);

	public static LocString NeutralCardAddedAnnotation() =>
		new(Table, NeutralCardAddedAnnotationKey);

	public static LocString NeutralPotionModifiedAnnotation() =>
		new(Table, NeutralPotionModifiedAnnotationKey);

	public static IHoverTip PlayRateHoverTip() =>
		new HoverTip(
			new LocString(Table, PlayRateTitleKey),
			new LocString(Table, PlayRateAnnotationKey));
}
