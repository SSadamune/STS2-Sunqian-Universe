using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 卡牌自行声明是否满足 <see cref="LoudSecretPlotPower"/> 的「随机多名敌人」目标意图。
/// <see cref="TargetType.RandomEnemy"/> 本身不构成触发条件；须通过此接口 opt-in（如飞火流星+ 且 X&gt;1）。
/// </summary>
public interface ISoloMultitargetReplayOptIn
{
	bool QualifiesForSoloMultitargetReplay();
}
