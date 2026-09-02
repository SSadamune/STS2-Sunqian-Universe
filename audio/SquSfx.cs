#nullable enable
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Audio;

namespace Squ.Audio;

/// <summary>
/// 模组短音效：加载 <c>sunqian_universe.bank</c>，用原版 <see cref="SfxCmd.Play"/> 播 FMOD 事件。
/// </summary>
internal static class SquSfx
{
	public const string BankPath = "res://audio/sunqian_universe.bank";
	public const string GuidsPath = "res://audio/GUIDs.txt";

	public const string TenThousandTransparentHolesEvent = "event:/sunqian_universe/sfx/一万个透明窟窿";
	public const string DualSwordsEvent = "event:/sunqian_universe/sfx/一把叫仁之剑，一把叫义之剑";
	public const string ThreeHoursBreakJingzhouEvent = "event:/sunqian_universe/sfx/三个时辰破荆州";
	public const string EightHundredLiEvent = "event:/sunqian_universe/sfx/三天内纵横八百里";
	public const string GrieveForLordEvent = "event:/sunqian_universe/sfx/为主公悲伤";
	public const string RighteousnessSwordEvent = "event:/sunqian_universe/sfx/义之剑";
	public const string BenevolenceSwordEvent = "event:/sunqian_universe/sfx/仁之剑";
	public const string WhoseHoundsEvent = "event:/sunqian_universe/sfx/哪家的鹰犬";
	public const string TraitorDongZhuoEvent = "event:/sunqian_universe/sfx/国贼董卓";
	public const string GrieveThenCongratulateLordEvent = "event:/sunqian_universe/sfx/在下一者为主公悲伤，二者给主公道喜";
	public const string WhatDoWeEatEvent = "event:/sunqian_universe/sfx/我们吃什么";
	public const string ExactlyWhatToEatEvent = "event:/sunqian_universe/sfx/是啊吃什么";
	public const string DeniedEvent = "event:/sunqian_universe/sfx/竟然不许";
	public const string CongratulateLordEvent = "event:/sunqian_universe/sfx/给主公道喜";
	public const string ConsecutiveSiegesEvent = "event:/sunqian_universe/sfx/连续攻城拔寨";
	public const string TransparentHoleEvent = "event:/sunqian_universe/sfx/透明窟窿";
	public const string LuKangUndefendedEvent = "event:/sunqian_universe/sfx/陆康未设防";

	public static void Register()
	{
		FmodStudioDeferredBankRegistration.RegisterBank(BankPath);
		FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings(GuidsPath);
	}

	public static void Play(string eventPath)
	{
		SfxCmd.Play(eventPath);
	}
}
