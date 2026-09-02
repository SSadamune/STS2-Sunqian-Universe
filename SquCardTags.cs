using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Powers;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ;

[RegisterOwnedCardTag("script")]
[RegisterOwnedCardTag("burning")]
public static class SquCardTags
{
	public static readonly CardTag Script = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "script")
		.GetModCardTag();

	/// <summary>能造成 <see cref="Squ.Powers.BurningPower"/> 的卡牌（供火种等效果识别）。</summary>
	public static readonly CardTag Burning = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "burning")
		.GetModCardTag();

	/// <summary>
	/// 能造成 <see cref="Squ.Powers.BurningPower"/> 的卡牌。
	/// 夜袭乌巢生效时，其持有者的 <see cref="CardTag.Strike"/> 攻击牌也视为灼烧牌，
	/// 但不会修改卡牌的实际标签集合。
	/// </summary>
	public static bool AppliesBurning(CardModel card) =>
		card.Tags.Contains(Burning) || HasNightRaidBurning(card);

	private static bool HasNightRaidBurning(CardModel card) =>
		card.IsMutable
		&& card.Type == CardType.Attack
		&& card.Tags.Contains(CardTag.Strike)
		&& card.Owner?.Creature?.GetPower<ScriptNightRaidWuchaoPower>() is { Amount: > 0 };
}
