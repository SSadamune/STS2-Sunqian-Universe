using MegaCrit.Sts2.Core.Entities.Cards;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "what_to_eat")]
public sealed class WhatToEat : WhatToEatCardBase
{
	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.SingleplayerOnly;
}
