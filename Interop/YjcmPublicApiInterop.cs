using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// Optional proxy for the public gameplay API exposed by the Yjcm mod.
/// Keep these fallback stubs simple: RitsuLib replaces them only when Yjcm is loaded.
/// </summary>
[ModInterop("Yjcm", "Yjcm.Api.YjcmPublicApi")]
public static class YjcmPublicApiInterop
{
	public static bool IsReady => false;

	public static bool IsYjcmCharacter(CharacterModel? character) => false;

	public static bool IsHuoGongPower(PowerModel? power) => false;

	public static IHoverTip CreateHuoGongHoverTip() => null!;
}
