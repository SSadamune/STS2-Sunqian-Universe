using MegaCrit.Sts2.Core.Entities.Cards;
using Squ;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ;

[RegisterOwnedCardTag("script")]
public static class SquCardTags
{
	public static readonly CardTag Script = ModContentRegistry
		.GetQualifiedCardTagId(SquMod.ModId, "script")
		.GetModCardTag();
}
