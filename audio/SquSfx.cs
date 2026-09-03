#nullable enable
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Audio;
using STS2RitsuLib.RunRngs;

namespace Squ.Audio;

/// <summary>
/// 模组短音效：加载 <c>sunqian_universe.bank</c>，用原版 <see cref="SfxCmd.Play"/> 播 FMOD 事件。
/// </summary>
internal static class SquSfx
{
	public const string BankPath = "res://audio/sunqian_universe.bank";
	public const string GuidsPath = "res://audio/GUIDs.txt";
	private const string SfxRngStreamId = "sfx";

	public const string HoldNanJunAloneEvent = "event:/sunqian_universe/sfx/一人坚守南郡城";
	public const string DualSwordsEvent = "event:/sunqian_universe/sfx/一把叫仁之剑，一把叫义之剑";
	public const string WontBePoliteEvent = "event:/sunqian_universe/sfx/不会客气";
	public const string SpareTheYoungAndOldEvent = "event:/sunqian_universe/sfx/不斩老幼";
	public const string GrieveForLordEvent = "event:/sunqian_universe/sfx/为主公悲伤";
	public const string RighteousnessSwordEvent = "event:/sunqian_universe/sfx/义之剑";
	public const string ChaosHarmedYouNotAmanEvent = "event:/sunqian_universe/sfx/乱世害你-不是阿瞒害了你";
	public const string ChaosHarmedYouNotDieInVainEvent = "event:/sunqian_universe/sfx/乱世害你-不能白死";
	public const string TwoWordHowToRelieveWorryEvent = "event:/sunqian_universe/sfx/二言-何以解忧";
	public const string TwoWordBitterDaysEvent = "event:/sunqian_universe/sfx/二言-去日苦多";
	public const string TwoWordOnlyDukangEvent = "event:/sunqian_universe/sfx/二言-唯有杜康";
	public const string TwoWordWineAndSongEvent = "event:/sunqian_universe/sfx/二言-对酒当歌人生几何";
	public const string TwoWordUnforgettableWorryEvent = "event:/sunqian_universe/sfx/二言-忧思难忘";
	public const string TwoWordGenerousAndStrongEvent = "event:/sunqian_universe/sfx/二言-慨当以慷";
	public const string TwoWordLikeMorningDewEvent = "event:/sunqian_universe/sfx/二言-譬如朝露";
	public const string HumanTransmutationEvent = "event:/sunqian_universe/sfx/人体炼成";
	public const string BenevolenceSwordEvent = "event:/sunqian_universe/sfx/仁之剑";
	public const string WhoAreYouEvent = "event:/sunqian_universe/sfx/你是何人";
	public const string RunWildAgainEvent = "event:/sunqian_universe/sfx/再来撒野";
	public const string HuaguMianzhangEvent = "event:/sunqian_universe/sfx/化骨绵掌";
	public const string ForkOutWalkEvent = "event:/sunqian_universe/sfx/叉出去-走";
	public const string ForkOutGetUpEvent = "event:/sunqian_universe/sfx/叉出去-起来";
	public const string WhoseHoundsEvent = "event:/sunqian_universe/sfx/哪家的鹰犬";
	public const string TraitorDongZhuoEvent = "event:/sunqian_universe/sfx/国贼董卓";
	public const string GrieveThenCongratulateLordEvent = "event:/sunqian_universe/sfx/在下一者为主公悲伤，二者给主公道喜";
	public const string CunningYuanShuWhyPursueEvent = "event:/sunqian_universe/sfx/多智袁术-为何追杀";
	public const string CunningYuanShuGreedyGloryEvent = "event:/sunqian_universe/sfx/多智袁术-贪功心切";
	public const string LoyaltyOverKinEvent = "event:/sunqian_universe/sfx/大义灭亲";
	public const string YilingFineFireEvent = "event:/sunqian_universe/sfx/夷陵好火";
	public const string HouseServantsComeEvent = "event:/sunqian_universe/sfx/家丁剧本-来人";
	public const string FingerStrikeDongZhuoHeadEvent = "event:/sunqian_universe/sfx/弹指打击-取董贼首级";
	public const string NearAndFarEvent = "event:/sunqian_universe/sfx/忽近忽远";
	public const string SlamTheCommandDeskEvent = "event:/sunqian_universe/sfx/怒掀帅案";
	public const string WhatDoWeEatEvent = "event:/sunqian_universe/sfx/我们吃什么";
	public const string BiggerGobletEvent = "event:/sunqian_universe/sfx/换大盏";
	public const string ExactlyWhatToEatEvent = "event:/sunqian_universe/sfx/是啊吃什么";
	public const string EmperorKnowsNoWarEvent = "event:/sunqian_universe/sfx/朕不知兵";
	public const string WaterFireInvincibleEvent = "event:/sunqian_universe/sfx/水火无敌";
	public const string BombardChibiReduceAttackEvent = "event:/sunqian_universe/sfx/炮轰赤壁-减攻";
	public const string BombardChibiIgniteEvent = "event:/sunqian_universe/sfx/炮轰赤壁-点火";
	public const string FateUnknownEvent = "event:/sunqian_universe/sfx/生死不明";
	public const string SilverUprisingEvent = "event:/sunqian_universe/sfx/白银起义";
	public const string DeniedEvent = "event:/sunqian_universe/sfx/竟然不许";
	public const string ArrowCurtainEvent = "event:/sunqian_universe/sfx/箭幕";
	public const string CongratulateLordEvent = "event:/sunqian_universe/sfx/给主公道喜";
	public const string SelfDecapitationEvent = "event:/sunqian_universe/sfx/自刎归天";
	public const string ExecutionerArchersReadyEvent = "event:/sunqian_universe/sfx/行刑官-弓箭手准备";
	public const string ExecutionerLooseArrowsEvent = "event:/sunqian_universe/sfx/行刑官-放箭";
	public const string XiliangSavageEvent = "event:/sunqian_universe/sfx/西凉野人";
	public const string CloseFittingArmorEvent = "event:/sunqian_universe/sfx/贴身铠甲";
	public const string TransparentHoleGuanYuEvent = "event:/sunqian_universe/sfx/透明窟窿-戳关羽";
	public const string TransparentHoleZhouYuEvent = "event:/sunqian_universe/sfx/透明窟窿-戳周瑜";
	public const string TransparentHoleMaChaoEvent = "event:/sunqian_universe/sfx/透明窟窿-戳马超";
	public const string TransparentHoleLuBuEvent = "event:/sunqian_universe/sfx/透明窟窿-捅吕布";
	public const string TransparentHoleYuanShuEvent = "event:/sunqian_universe/sfx/透明窟窿-捅袁术";
	public const string BlitzkriegThreeHoursBreakJingzhouEvent = "event:/sunqian_universe/sfx/闪电战-三个时辰破荆州";
	public const string BlitzkriegThreeDaysEightHundredLiEvent = "event:/sunqian_universe/sfx/闪电战-三天内纵横八百里";
	public const string BlitzkriegHahahaEvent = "event:/sunqian_universe/sfx/闪电战-哈哈哈哈哈";
	public const string BlitzkriegRushToLujiangEvent = "event:/sunqian_universe/sfx/闪电战-星夜兼程奔赴庐江";
	public const string BlitzkriegConsecutiveSiegesEvent = "event:/sunqian_universe/sfx/闪电战-连续攻城拔寨";
	public const string BlitzkriegLuKangUndefendedEvent = "event:/sunqian_universe/sfx/闪电战-陆康未设防";
	public const string LuXunCybertronEvent = "event:/sunqian_universe/sfx/陆逊塞伯坦";
	public const string FlyingFireMeteor1Event = "event:/sunqian_universe/sfx/飞火流星1";
	public const string FlyingFireMeteor2Event = "event:/sunqian_universe/sfx/飞火流星2";
	public const string FlyingFireMeteor3Event = "event:/sunqian_universe/sfx/飞火流星3";
	public const string FlyingFireMeteor4Event = "event:/sunqian_universe/sfx/飞火流星4";
	public const string GoldenUprisingEvent = "event:/sunqian_universe/sfx/黄金起义";

	public static void Register()
	{
		FmodStudioDeferredBankRegistration.RegisterBank(BankPath);
		FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings(GuidsPath);
	}

	public static void Play(string eventPath)
	{
		SfxCmd.Play(eventPath);
	}

	/// <summary>
	/// 等概率播放其中一个事件。使用本 Mod 独立的跑局 RNG 流（<c>sfx</c>），
	/// 与 <c>CombatTargets</c>、<c>Shuffle</c>、<c>Niche</c>、奖励/商店等原版序列互不影响。
	/// </summary>
	public static void PlayRandom(IRunState? runState, params string[] eventPaths)
	{
		if (eventPaths.Length == 0)
		{
			return;
		}

		int index = 0;
		if (eventPaths.Length > 1 && TryGetSfxRng(runState, out Rng rng))
		{
			index = rng.NextInt(eventPaths.Length);
		}

		Play(eventPaths[index]);
	}

	private static bool TryGetSfxRng(IRunState? runState, out Rng rng)
	{
		if (runState is RunState concrete)
		{
			rng = ModRunRngRegistry.Get(concrete, SquMod.ModId, SfxRngStreamId);
			return true;
		}

		rng = null!;
		return false;
	}
}
