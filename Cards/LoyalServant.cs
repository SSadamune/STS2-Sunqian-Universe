using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "loyal_servant")]
public sealed class LoyalServant : FamilyCardTemplate
{
	protected override int BlockAmount => 5;

	protected override int DrawCards => 0;
}
