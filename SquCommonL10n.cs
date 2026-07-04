using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;

namespace Squ;

/// <summary>
/// 跨卡牌/能力复用的 mod 自有本地化表（<see cref="SquMod.CommonL10nStem"/>）。
/// </summary>
public static class SquCommonL10n
{
	public const string AnnotationTitleKey = "SUNQIAN_UNIVERSE_COMMON.ANNOTATION.title";

	public static string Table =>
		RitsuLibFramework.GetI18NLocTableId(SquMod.ModId, SquMod.CommonL10nStem);

	public static LocString AnnotationTitle() => new(Table, AnnotationTitleKey);
}
