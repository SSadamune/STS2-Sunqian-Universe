#nullable enable
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "summon_third_brother")]
public sealed class SummonThirdBrother : BrotherSummonCard
{
	protected override string CompanionId => CompanionSummons.ThirdBrotherId;
	public override CardPoolModel VisualCardPool => ModelDb.CardPool<LiubeiCardPool>();
	public SummonThirdBrother() : base(CardRarity.Token) { }
}
