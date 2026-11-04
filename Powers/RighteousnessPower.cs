#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.RunData;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class RighteousnessPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override bool AllowNegative => true;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPower.png",
		BigIconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards)];

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
		card.Owner.Creature == Owner && card.Tags.Contains(CardTag.Strike)
			? playCount + Math.Max(0, Amount / 10)
			: playCount;

	public override async Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner.Creature != Owner || !card.Tags.Contains(CardTag.Strike) || Amount < 10)
		{
			return;
		}
		int spent = Amount / 10 * 2;
		Flash();
		await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -spent, Owner, card);
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,
		PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power == this && Owner.Player is { } player)
		{
			int previous = RighteousnessRunData.Get(player);
            if (Amount < previous) RighteousnessRunData.RecordLoss(player, (long)previous - Amount);
			RighteousnessRunData.Set(player, Amount);
		}
		return Task.CompletedTask;
	}

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (Amount != 0 && oldOwner.Player is { } player && CombatManager.Instance.IsInProgress
            && !CombatManager.Instance.IsOverOrEnding)
        {
            RighteousnessRunData.RecordLoss(player, Math.Max(0, RighteousnessRunData.Get(player)));
            RighteousnessRunData.Set(player, 0);
        }
        return Task.CompletedTask;
    }
}
