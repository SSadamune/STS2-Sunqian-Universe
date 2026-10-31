using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 文关羽：获得敏捷直到失效；回合开始失去格挡时，对所有敌人造成等量伤害。
/// </summary>
[RegisterPower]
public sealed class GuanYuCivilVerPower : ScriptPowerTemplate
{
    private decimal _grantedDexterity;

    private sealed class Data
    {
        public decimal BlockAtTurnStart { get; set; }
        public bool WatchingBlockClear { get; set; }
    }

    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;

    protected override object InitInternalData() => new Data();

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuCivilVerPower.png",
        BigIconPath: "res://images/powers/GuanYuCivilVerPowerBig.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount <= 0m)
        {
            return;
        }

        _grantedDexterity = Amount;
        await PowerCmd.Apply<DexterityPower>(
            ActivationChoiceContext,
            Owner,
            Amount,
            Owner,
            cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_grantedDexterity > 0m && !oldOwner.IsDead)
        {
            await PowerCmd.Apply<DexterityPower>(
                new ThrowingPlayerChoiceContext(),
                oldOwner,
                -_grantedDexterity,
                oldOwner,
                null);
        }

        await base.AfterRemoved(oldOwner);
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        Data data = GetInternalData<Data>();
        data.WatchingBlockClear = side == Owner.Side && participants.Contains(Owner) && !Owner.IsDead;
        data.BlockAtTurnStart = data.WatchingBlockClear ? Owner.Block : 0m;
        return Task.CompletedTask;
    }

    public override async Task AfterBlockCleared(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        decimal lost = data.WatchingBlockClear
            ? data.BlockAtTurnStart - decimal.Max(0m, Owner.Block)
            : 0m;
        data.WatchingBlockClear = false;
        data.BlockAtTurnStart = 0m;
        if (lost <= 0m || Owner.IsDead)
        {
            return;
        }

        List<Creature> enemies = Owner.CombatState?.HittableEnemies.ToList() ?? [];
        if (enemies.Count == 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            enemies,
            lost,
            ValueProp.Unpowered,
            Owner);
    }
}
