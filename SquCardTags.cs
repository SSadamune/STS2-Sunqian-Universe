using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ;
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

	/// <summary>能造成 <see cref="Squ.Powers.BurningPower"/> 的卡牌（供燃料充足等效果识别）。</summary>
	public static readonly CardTag Burning = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "burning")
		.GetModCardTag();

	public static bool AppliesBurning(CardModel card) => card.Tags.Contains(Burning);
}
