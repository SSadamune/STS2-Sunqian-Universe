using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "next_of_kin")]
public sealed class NextOfKin : FamilyCardTemplate
{
	protected override int BlockAmount => 0;

	protected override int DrawCards => 2;
}
