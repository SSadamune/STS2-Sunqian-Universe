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

	public static readonly CardTag Script = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "script")
		.GetModCardTag();

	/// <summary>能造成 <see cref="BurningPower"/> 的卡牌（供火种等效果识别）。</summary>
	public static readonly CardTag Burning = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "burning")
		.GetModCardTag();

	/// <summary>
	/// 标记强绑定所属角色机制、不应被《轧戏》或《人体炼成》发现的卡牌。
	/// </summary>
	public static readonly CardTag CharacterMechanicBound =
		CharacterMechanicBoundId.GetModCardTag();

	/// <summary>判断卡牌是否被标记为强绑定所属角色机制。</summary>
	public static bool IsCharacterMechanicBound(CardModel? card) =>
		card?.Tags.Contains(CharacterMechanicBound) == true;

	/// <summary>
	/// 能造成 <see cref="BurningPower"/> 的卡牌。
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
