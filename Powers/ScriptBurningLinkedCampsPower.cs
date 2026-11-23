#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptBurningLinkedCampsPower : ScriptPowerTemplate
{
    public const int BurnBonusPercent = 10;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/ScriptBurningLinkedCampsPower.png",
        BigIconPath: "res://images/powers/ScriptBurningLinkedCampsPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Burn>(), HoverTipFactory.FromPower<BurningPower>()];
    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner && cardSource is Burn && Owner.GetPower<BurningPower>()?.IsResolvingDamage != true
            ? AddBonus(context, result.TotalDamage + result.OverkillDamage, cardSource)
            : Task.CompletedTask;

    internal Task AfterBurningDamage(PlayerChoiceContext context, IEnumerable<DamageResult> results,
        CardModel? cardSource) => AddBonus(context,
            results.Where(result => result.Receiver == Owner).Sum(result => result.TotalDamage + result.OverkillDamage), cardSource);

    private async Task AddBonus(PlayerChoiceContext context, int damage, CardModel? cardSource)
    {
        if (damage <= 0) return;
        Flash();
        decimal bonus = (decimal)damage * Amount;
        if (Owner.GetPower<GoodFirePower>() is { } goodFire)
            await PowerCmd.ModifyAmount(context, goodFire, bonus, Owner, cardSource);
        else
            await PowerCmd.Apply<PendingGoodFirePower>(context, Owner, bonus, Owner, cardSource);
    }
}
