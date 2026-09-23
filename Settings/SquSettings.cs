#nullable enable
using System;
using STS2RitsuLib.Data;

namespace Squ.Settings;

/// <summary>
/// 模组设置的持久化模型。滑条按分贝二次曲线（指数振幅）映射，叠在游戏音效滑条之上。
/// </summary>
public sealed class SquSettings
{
	public const string DataKey = "settings";
	public const string FileName = "settings.json";

	public const int MinSfxVolumePercent = 0;
	public const int MaxSfxVolumePercent = 100;
	public const int DefaultSfxVolumePercent = 50;

	public const NeutralPotionModificationMode DefaultNeutralPotionModificationMode =
		NeutralPotionModificationMode.WhenModCharacterPresent;

	/// <summary>
	/// 100% 时的线性倍率，也是实例音量在削波前大致还能往上走的上限。
	/// </summary>
	public const float PeakSfxGain = 8f;

	/// <summary>
	/// 0% 以上、50% 以下的分贝地板（相对 50%）。
	/// </summary>
	public const float MuteFloorDb = -24f;

	/// <summary>
	/// 100% 相对 50% 的分贝提升。50% = Peak / 10^(MaxBoostDb/20)，把余量留在后半段。
	/// </summary>
	public const float MaxBoostDb = 10f;

	public int SfxVolumePercent { get; set; } = DefaultSfxVolumePercent;

	public string PreviewSfxId { get; set; } = SquSfxPreview.DefaultId;

	public NeutralPotionModificationMode NeutralPotionModification { get; set; } =
		DefaultNeutralPotionModificationMode;

	/// <summary>
	/// 传给 <c>SfxCmd.Play</c> 的线性倍率。设置 50% 为 0 dB，100% 为 <see cref="PeakSfxGain"/>。
	/// </summary>
	public static float SfxLinearMultiplier
	{
		get
		{
			int percent = Math.Clamp(
				ModDataStore.For(SquMod.ModId).Get<SquSettings>(DataKey).SfxVolumePercent,
				MinSfxVolumePercent,
				MaxSfxVolumePercent);
			return PercentToLinearMultiplier(percent);
		}
	}

	/// <summary>
	/// 0% 静音；其余分贝为过 0%⁺ / 50% / 100% 的二次曲线。
	/// 50% 为 0 dB；100% 为 +<see cref="MaxBoostDb"/> dB（<see cref="PeakSfxGain"/>）。
	/// </summary>
	public static float PercentToLinearMultiplier(int percent)
	{
		float t = Math.Clamp(percent, MinSfxVolumePercent, MaxSfxVolumePercent) / 100f;
		if (t <= 0f)
		{
			return 0f;
		}

		float unityGain = PeakSfxGain / MathF.Pow(10f, MaxBoostDb / 20f);
		return unityGain * MathF.Pow(10f, DbOffsetAt(t) / 20f);
	}

	/// <summary>
	/// 相对 50% 的分贝：<c>t=0</c> 为 <see cref="MuteFloorDb"/>，<c>t=0.5</c> 为 0，<c>t=1</c> 为 <see cref="MaxBoostDb"/>。
	/// </summary>
	private static float DbOffsetAt(float t)
	{
		const float unityT = DefaultSfxVolumePercent / 100f;
		const float span = MaxBoostDb - MuteFloorDb;
		const float b = (-MuteFloorDb - unityT * unityT * span) / (unityT * (1f - unityT));
		const float c = span - b;
		return MuteFloorDb + (b + c * t) * t;
	}
}
