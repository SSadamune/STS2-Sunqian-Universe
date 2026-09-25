#nullable enable
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using Squ.Settings;
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
	public const string GreatestPassOfCentralPlainsEvent = "event:/sunqian_universe/sfx/中原第一雄关";
	public const string WhyDoubtWeiTooCautiousEvent = "event:/sunqian_universe/sfx/何疑魏-丞相太过于谨小慎微了";
	public const string WhyDoubtWeiSoCautiousEvent = "event:/sunqian_universe/sfx/何疑魏-想不到丞相竟是这般谨慎";
	public const string WhyDoubtWeiEvent = "event:/sunqian_universe/sfx/何疑魏-魏延早就把三军看成他自己的了";
	public const string KnowWrongDenyWrongEvent = "event:/sunqian_universe/sfx/何谓人主，那就是知错改错不认错";
	public const string RespectEldersTooOldEvent = "event:/sunqian_universe/sfx/尊老爱幼-你太老了";
	public const string RespectEldersSpareTheYoungAndOldEvent = "event:/sunqian_universe/sfx/尊老爱幼-不斩老幼";
	public const string NotAfraidOfAcidEvent = "event:/sunqian_universe/sfx/不怕酸-咱家不怕酸";
	public const string SaidNotAfraidOfAcidEvent = "event:/sunqian_universe/sfx/不怕酸-说了不怕酸";
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
	public const string PortalJizhouJingzhouEvent = "event:/sunqian_universe/sfx/传送门-冀州和荆州有空间通道";
	public const string PortalGansuHenanEvent = "event:/sunqian_universe/sfx/传送门-星夜从甘肃杀到河南";
	public const string PortalYuanShaoXuzhouEvent = "event:/sunqian_universe/sfx/传送门-袁绍兵发徐州";
	public const string PortalXiliangChenliuEvent = "event:/sunqian_universe/sfx/传送门-西凉太守来陈留会盟";
	public const string PortalChanganMeiwuEvent = "event:/sunqian_universe/sfx/传送门-长安和堳坞之间有传送门";
	public const string VoiceChangeObviousEvent = "event:/sunqian_universe/sfx/变声期-有个很明显的变声期";
	public const string VoiceChangeYearsAgoEvent = "event:/sunqian_universe/sfx/变声期-这个多年前，明显有变声期";
	public const string VoiceChangeTheseWordsEvent = "event:/sunqian_universe/sfx/变声期-这几个字儿明显是变声期啊";
	public const string VoiceChangeVolumeWrongEvent = "event:/sunqian_universe/sfx/变声期-音量不对";
	public const string WhoseHoundsEvent = "event:/sunqian_universe/sfx/哪家的鹰犬";
	public const string TraitorDongZhuoEvent = "event:/sunqian_universe/sfx/国贼董卓";
	public const string CunningYuanShuWhyPursueEvent = "event:/sunqian_universe/sfx/多智袁术-为何追杀";
	public const string CunningYuanShuGreedyGloryEvent = "event:/sunqian_universe/sfx/多智袁术-贪功心切";
	public const string LoyaltyOverKinEvent = "event:/sunqian_universe/sfx/大义灭亲";
	public const string YilingFineFireEvent = "event:/sunqian_universe/sfx/夷陵好火";
	public const string HouseServantsComeEvent = "event:/sunqian_universe/sfx/家丁剧本-来人";
	public const string ThunderOnStillLakeAsLongAsIBreatheEvent = "event:/sunqian_universe/sfx/平湖惊雷-只要一息尚存";
	public const string ThunderOnStillLakeChopYourHeadEvent = "event:/sunqian_universe/sfx/平湖惊雷-放肆我砍你的头";
	public const string FingerStrikeRemainingLandEvent = "event:/sunqian_universe/sfx/弹指打击-剩下一小片江山";
	public const string FingerStrikeDongZhuoHeadEvent = "event:/sunqian_universe/sfx/弹指打击-取董贼首级";
	public const string NearAndFarEvent = "event:/sunqian_universe/sfx/忽近忽远";
	public const string SlamTheCommandDeskEvent = "event:/sunqian_universe/sfx/怒掀帅案";
	public const string WhatDoWeEatEvent = "event:/sunqian_universe/sfx/我们吃什么";
	public const string BiggerGobletEvent = "event:/sunqian_universe/sfx/换大盏-换大盏";
	public const string BiggerGobletWontBePoliteEvent = "event:/sunqian_universe/sfx/换大盏-不会客气";
	public const string BiggerGobletToastEvent = "event:/sunqian_universe/sfx/换大盏-当浮一大白";
	public const string ExactlyWhatToEatEvent = "event:/sunqian_universe/sfx/是啊吃什么";
	public const string EmperorKnowsNoWarEvent = "event:/sunqian_universe/sfx/朕不知兵";
	public const string WaterFireInvincibleEvent = "event:/sunqian_universe/sfx/水火无敌";
	public const string BombardChibiReduceAttackEvent = "event:/sunqian_universe/sfx/炮轰赤壁-减攻";
	public const string BombardChibiRestoreEnergyEvent = "event:/sunqian_universe/sfx/炮轰赤壁-我笑猪哥愚蠢";
	public const string BombardChibiIgniteEvent = "event:/sunqian_universe/sfx/炮轰赤壁-点火";
	public const string FateUnknownEvent = "event:/sunqian_universe/sfx/生死不明";
	public const string FateUnknownThatsDeathEvent = "event:/sunqian_universe/sfx/生死不明-那就是死了";
	public const string SilverUprisingEvent = "event:/sunqian_universe/sfx/白银起义";
	public const string DeniedEvent = "event:/sunqian_universe/sfx/竟然不许";
	public const string ArrowCurtainEvent = "event:/sunqian_universe/sfx/箭幕";
	public const string CelebrateMourningCongratulateLordEvent = "event:/sunqian_universe/sfx/闻丧贺喜-给主公道喜";
	public const string CelebrateMourningGrieveForLordEvent = "event:/sunqian_universe/sfx/闻丧贺喜-为主公悲伤";
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
	public const string BlockFirefightingDoNotDisturbEvent = "event:/sunqian_universe/sfx/阻拦救火-不可惊扰";
	public const string BlockFirefightingSageEvent = "event:/sunqian_universe/sfx/阻拦救火-先生奇人";
	public const string MonsterHuntingMethodEnemyEvent = "event:/sunqian_universe/sfx/过禽论-若除禽兽";
	public const string MonsterHuntingMethodSelfEvent = "event:/sunqian_universe/sfx/过禽论-与禽兽何异";
	public const string MonsterHuntingMethodOtherPlayerEvent = "event:/sunqian_universe/sfx/过禽论-小女怕配不上吕将军";
	public const string MonsterHuntingMethodOtherEvent = "event:/sunqian_universe/sfx/过禽论-义父要把我";
	public const string KeepCalmHateCloudsJudgmentEvent = "event:/sunqian_universe/sfx/保持冷静-仇恨会使你丧失判断力";
	public const string KeepCalmDoNotAngerEvent = "event:/sunqian_universe/sfx/保持冷静-不要愤怒愤怒会降低智慧";
	public const string KeepCalmLuBuMereMortalEvent = "event:/sunqian_universe/sfx/保持冷静-吕布一介匹夫";
	public const string KeepCalmXuzhouWasMineEvent = "event:/sunqian_universe/sfx/保持冷静-徐州原本就是我哒";
	public const string LaserSwordAssassinationDrawEvent = "event:/sunqian_universe/sfx/光剑刺杀-拔刀";
	public const string LaserSwordAssassinationLaserEvent = "event:/sunqian_universe/sfx/光剑刺杀-激光";
	public const string DigRaidEvent = "event:/sunqian_universe/sfx/掘地突袭";
	public const string MusouAttackThoughtLuBuEvent = "event:/sunqian_universe/sfx/无双乱舞-我原本以为吕布";
	public const string RuthlessStrikeDontForceMeEvent = "event:/sunqian_universe/sfx/无情打击-可别逼我使出无情剑来";
	public const string RuthlessStrikeSwordSoundEvent = "event:/sunqian_universe/sfx/无情打击-剑声";
	public const string KeepPlayingKeepDancingEvent = "event:/sunqian_universe/sfx/接着奏乐接着舞-接着奏乐接着舞";
	public const string WorkLifeBalanceLetMeEnjoyEvent = "event:/sunqian_universe/sfx/劳逸结合-我打了一辈子仗就不能享受享受吗";
	public const string BurnAfterReadingPlayEvent = "event:/sunqian_universe/sfx/阅后即焚-读完一卷烧一卷，全部读完则全部烧尽";
	public const string BurnAfterReadingTriggerBurnOneEvent = "event:/sunqian_universe/sfx/阅后即焚-读完一卷烧一卷";
	public const string BurnAfterReadingTriggerBurnAllEvent = "event:/sunqian_universe/sfx/阅后即焚-全部读完则全部烧尽";
	public const string BurnAfterReadingTriggerEmptyStudyEvent = "event:/sunqian_universe/sfx/阅后即焚-因此书房无书";
	public const string LensReshootShowHeWasThereEvent = "event:/sunqian_universe/sfx/镜头补拍-为了表现他此时在场";
	public const string LensReshootGoodSpiritEvent = "event:/sunqian_universe/sfx/镜头补拍-好，好志气";
	public const string LensReshootZhaoYunDidntComeEvent = "event:/sunqian_universe/sfx/镜头补拍-说明赵云没来";
	public const string WatchFireFromShoreNoRudenessEvent = "event:/sunqian_universe/sfx/隔岸观火-三弟休得无礼";
	public const string WatchFireFromShoreStopTalkingEvent = "event:/sunqian_universe/sfx/隔岸观火-三弟快快住口";
	public const string WatchFireFromShoreKneelEvent = "event:/sunqian_universe/sfx/隔岸观火-放肆，还不快跪下";
	public const string WatchFireFromShoreYideEvent = "event:/sunqian_universe/sfx/隔岸观火-翼德，不可出言不逊";
	public const string TooKindToBeTrueEvent = "event:/sunqian_universe/sfx/长厚似伪-为何老替他说话";
	public const string StargazingWangYunEvent = "event:/sunqian_universe/sfx/夜观天象-王允";
	public const string StargazingDongZhuoEvent = "event:/sunqian_universe/sfx/夜观天象-董卓";

	public const string ChickenFootCheeseXuShuEvent = "event:/sunqian_universe/sfx/鸡脚芝士-徐庶";
	public const string ChickenFootCheeseTaoQianEvent = "event:/sunqian_universe/sfx/鸡脚芝士-陶谦";
	public const string ChickenFootCheeseLiuBeiEvent = "event:/sunqian_universe/sfx/鸡脚芝士-刘备";
	public const string ChickenFootCheeseChenGongEvent = "event:/sunqian_universe/sfx/鸡脚芝士-陈宫";
	public const string ChickenFootCheeseWangPingEvent = "event:/sunqian_universe/sfx/鸡脚芝士-王平";
	public const string SlightRevisionEvent = "event:/sunqian_universe/sfx/稍作修改-好方略，不过我想稍作修改";
	public const string SunqianHurtEvent = "event:/sunqian_universe/sfx/孙乾-受击";
	public const string BasicStrikeCaoCaoEvent = "event:/sunqian_universe/sfx/打击-曹操";
	public const string BasicStrikeLiuBeiEvent = "event:/sunqian_universe/sfx/打击-刘备";
	public const string BasicStrikeNailongEvent = "event:/sunqian_universe/sfx/打击-奶龙";
	public const string BasicStrikeZhangFeiEvent = "event:/sunqian_universe/sfx/打击-张飞";
	public const string BasicDefendCaoCaoEvent = "event:/sunqian_universe/sfx/防御-曹操";
	public const string BasicDefendLiuBeiEvent = "event:/sunqian_universe/sfx/防御-刘备";
	public const string WineDrinkThisFlaskEvent = "event:/sunqian_universe/sfx/酒-喝下这壶酒";
	public const string WineThirstyEvent = "event:/sunqian_universe/sfx/酒-我正渴着呢";
	public const string WineOldHeroEvent = "event:/sunqian_universe/sfx/酒-酒是老英雄";
	public const string WineWentIntoTownEvent = "event:/sunqian_universe/sfx/酒-进城喝酒去了";

	public static readonly string[] BasicStrikeEvents =
	[
		BasicStrikeCaoCaoEvent,
		BasicStrikeLiuBeiEvent,
		BasicStrikeNailongEvent,
		BasicStrikeZhangFeiEvent,
	];

	public static readonly string[] BasicDefendEvents =
	[
		BasicDefendCaoCaoEvent,
		BasicDefendLiuBeiEvent,
	];

	public static readonly string[] WineEvents =
	[
		WineDrinkThisFlaskEvent,
		WineThirstyEvent,
		WineOldHeroEvent,
		WineWentIntoTownEvent,
	];

	public static readonly string[] VoiceChangeEvents =
	[
		VoiceChangeYearsAgoEvent,
		VoiceChangeObviousEvent,
		VoiceChangeTheseWordsEvent,
		VoiceChangeVolumeWrongEvent,
	];

	public static readonly string[] FlyingFireMeteorEvents =
	[
		FlyingFireMeteor1Event,
		FlyingFireMeteor2Event,
		FlyingFireMeteor3Event,
		FlyingFireMeteor4Event,
	];

	public static readonly string[] ChickenFootCheeseEvents =
	[
		ChickenFootCheeseXuShuEvent,
		ChickenFootCheeseTaoQianEvent,
		ChickenFootCheeseLiuBeiEvent,
		ChickenFootCheeseWangPingEvent,
	];

	public static readonly string[] TransparentHoleEvents =
	[
		TransparentHoleGuanYuEvent,
		TransparentHoleZhouYuEvent,
		TransparentHoleMaChaoEvent,
		TransparentHoleLuBuEvent,
		TransparentHoleYuanShuEvent,
	];

	public static readonly string[] TwoWordPoetTriggerEvents =
	[
		TwoWordHowToRelieveWorryEvent,
		TwoWordBitterDaysEvent,
		TwoWordOnlyDukangEvent,
		TwoWordUnforgettableWorryEvent,
		TwoWordGenerousAndStrongEvent,
		TwoWordLikeMorningDewEvent,
	];

	public static readonly string[] TwoWordPoetEvents =
	[
		TwoWordWineAndSongEvent,
		..TwoWordPoetTriggerEvents,
	];

	public static readonly string[] BurnAfterReadingTriggerEvents =
	[
		BurnAfterReadingTriggerBurnOneEvent,
		BurnAfterReadingTriggerBurnAllEvent,
		BurnAfterReadingTriggerEmptyStudyEvent,
	];

	public static readonly string[] BurnAfterReadingEvents =
	[
		BurnAfterReadingPlayEvent,
		..BurnAfterReadingTriggerEvents,
	];

	public static void Register()
	{
		FmodStudioDeferredBankRegistration.RegisterBank(BankPath);
		FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings(GuidsPath);
	}

	public static void Play(string eventPath)
	{
		SfxCmd.Play(eventPath, SquSettings.SfxLinearMultiplier);
	}

	/// <summary>
	/// 战斗结束结算（<see cref="CombatManager.IsEnding"/>）时仍要播放的语音。
	/// 原版 <see cref="SfxCmd.Play"/> 会在 IsEnding 时静默跳过。
	/// </summary>
	public static void PlayDuringCombatEnd(string eventPath)
	{
		if (NonInteractiveMode.IsActive)
		{
			return;
		}

		GameFmod.Studio.PlayOneShot(eventPath, SquSettings.SfxLinearMultiplier);
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
