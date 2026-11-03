#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class UnstableFactorPower : ModPowerTemplate
{
    private static readonly ConditionalWeakTable<ICombatState, HashSet<Player>> ProtectedPlayers = new();
    public static bool IsLifeLossDisabled(Player player) => player.Creature.CombatState is { } combat
        && ProtectedPlayers.TryGetValue(combat, out var players) && players.Contains(player);
    internal static void DisableLifeLoss(Player player) =>
        ProtectedPlayers.GetOrCreateValue(player.Creature.CombatState!).Add(player);
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("LifeLoss", 0)];
    public void AddLifeLoss() => DynamicVars["LifeLoss"].BaseValue += 2;
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/LifeContractPower.png",
        BigIconPath: "res://images/powers/LifeContractPowerBig.png");
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("LifeLoss", IsMutable && Owner.Player is { } player && IsLifeLossDisabled(player)
                ? 0 : IsMutable ? DynamicVars["LifeLoss"].IntValue : 2);
            text.Add("Summon", IsMutable ? Amount : 4);
            return text;
        }
    }
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Owner.Player is not { } player || !participants.Contains(Owner) || Owner.IsDead) return;
        var context = new BlockingPlayerChoiceContext();
        Flash();
        if (!IsLifeLossDisabled(player))
        {
            var hits = await CreatureCmd.Damage(context, Owner, DynamicVars["LifeLoss"].BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            LifeContractSystem.AddContribution(player, hits.Where(hit => hit.Receiver == Owner)
                .Sum(hit => hit.UnblockedDamage), CompanionSummons.ThirdBrotherId);
        }
        if (Owner.IsAlive)
            await CompanionSummons.SummonSpecific(context, player, CompanionSummons.ThirdBrotherId, Amount, this);
    }
}
