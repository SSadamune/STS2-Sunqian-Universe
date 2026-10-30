using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanYuCivilVerPower : ScriptPowerTemplate
{
    private sealed class Data { public int LastResolvedTurn { get; set; } = -1; }
    private sealed class OwnerEffectiveBlockVar() : DynamicVar("EffectiveBlock", 0)
    {
        private decimal CurrentValue => _owner is GuanYuCivilVerPower power ? power.EffectiveBlockAtTurnEnd : 0;
        protected override decimal GetBaseValueForIConvertible() => CurrentValue;
        public override string ToString() => ((int)CurrentValue).ToString(CultureInfo.InvariantCulture);
    }
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    protected override object InitInternalData() => new Data();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new OwnerEffectiveBlockVar(), new EnergyVar(1)];
    private int EnergySpentThisTurn => IsMutable && Owner.Player is { } player && CombatState is { } state
        ? TurnCardHistory.Get(state).EnergySpent(player)
        : 0;
    private decimal EffectiveBlockAtTurnEnd
    {
        get
        {
            int energy = EnergySpentThisTurn;
            return energy > 0 ? Math.Max(0, Amount * energy
                + (Owner.GetPower<DexterityPower>()?.Amount ?? 0)) : 0;
        }
    }
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuCivilVerPower.png",
        BigIconPath: "res://images/powers/GuanYuCivilVerPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.ForEnergy(this),
            HoverTipFactory.Static(StaticHoverTip.Block)];
    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
        {
            return;
        }
        int turn = Owner.Player?.PlayerCombatState?.TurnNumber ?? 0;
        var data = GetInternalData<Data>();
        if (data.LastResolvedTurn == turn)
        {
            return;
        }
        data.LastResolvedTurn = turn;
        decimal block = EffectiveBlockAtTurnEnd;
        if (block > 0)
        {
            Flash();
            await GainDexterityOnlyBlockAsync(Owner, block);
        }
    }

	private static async Task GainDexterityOnlyBlockAsync(
		Creature creature,
		decimal blockAmount)
	{
		if (CombatManager.Instance.IsOverOrEnding
			|| creature.IsDead
			|| creature.CombatState is not { } combatState)
		{
			return;
		}

		ValueProp props = ValueProp.Unpowered;
		await Hook.BeforeBlockGained(
			combatState,
			creature,
			blockAmount,
			props,
			null);
		await Hook.AfterModifyingBlockAmount(
			combatState,
			blockAmount,
			null,
			null,
			Array.Empty<AbstractModel>());

		SfxCmd.Play("event:/sfx/block_gain");
		VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_block");
		creature.GainBlockInternal(blockAmount);
		CombatManager.Instance.History.BlockGained(
			combatState,
			creature,
			(int)blockAmount,
			props,
			cardPlay: null);
		await Cmd.CustomScaledWait(0.1f, 0.25f);
		await Hook.AfterBlockGained(
			combatState,
			creature,
			blockAmount,
			props,
			null);
	}
}
