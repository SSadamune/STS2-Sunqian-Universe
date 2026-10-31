using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ;

[RegisterOwnedCardTag("script")]
[RegisterOwnedCardTag("burning")]
[RegisterOwnedCardTag("character_mechanic_bound")]
public static class SquCardTags
{
	/// <summary>“强绑定角色机制”卡牌标签的稳定限定 ID。</summary>
	public const string CharacterMechanicBoundId =
		"SUNQIAN_UNIVERSE_CARDTAG_CHARACTER_MECHANIC_BOUND";

	/// <summary>「灼烧」卡牌标签的稳定限定 ID。</summary>
	public const string BurningId = "SUNQIAN_UNIVERSE_CARDTAG_BURNING";

	public static readonly CardTag Script = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "script")
		.GetModCardTag();

	/// <summary>能造成 <see cref="BurningPower"/> 的卡牌（供火种等效果识别）。</summary>
	public static readonly CardTag Burning = BurningId.GetModCardTag();

	/// <summary>
	/// 标记强绑定所属角色机制、不应被《轧戏》或《人体炼成》发现的卡牌。
	/// </summary>
	public static readonly CardTag CharacterMechanicBound =
		CharacterMechanicBoundId.GetModCardTag();

	/// <summary>判断卡牌是否被标记为强绑定所属角色机制。</summary>
	public static bool IsCharacterMechanicBound(CardModel? card) =>
		card?.Tags.Contains(CharacterMechanicBound) == true;

	/// <summary>判断卡牌是否带有「灼烧」卡牌标签。</summary>
	public static bool IsBurningCard(CardModel? card) =>
		card?.Tags.Contains(Burning) == true;

	/// <summary>能直接给予灼烧的卡牌；剧本在伤害后追加的灼烧不属于牌本身的效果。</summary>
	public static bool AppliesBurning(CardModel card) => IsBurningCard(card);
}
