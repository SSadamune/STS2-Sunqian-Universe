using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class WakokuStudentPower : ModPowerTemplate
{
	private sealed class Data
	{
		public decimal Remainder;
		public decimal GrantedStrength;
		public decimal GrantedDexterity;
	}
	protected override object InitInternalData() => new Data();
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpThreshold", 9)];
	public override LocString Description
	{
		get
		{
			LocString text = base.Description;
			DynamicVars.AddTo(text);
			return text;
		}
	}
	public override PowerType Type => PowerType.Buff;
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
	public override PowerStackType StackType => PowerStackType.None;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/PhasingPower.png",
		BigIconPath: "res://images/powers/PhasingPowerBig.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.FromPower<StrengthPower>()];
	internal void Configure(int threshold) => DynamicVars["HpThreshold"].BaseValue = threshold;

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != Owner || delta >= 0) return;
		bool lethalLoss = Owner.CurrentHp <= 0;
		Data data = GetInternalData<Data>();
		data.Remainder -= delta;
		int threshold = DynamicVars["HpThreshold"].IntValue;
		decimal gain = decimal.Floor(data.Remainder / threshold);
		data.Remainder %= threshold;
		await Grant(gain);
		// Native HP-change hooks run before death prevention and revival. Include
		// thresholds crossed by this loss, then double this instance's history.
		if (lethalLoss)
			await Grant(data.GrantedStrength, data.GrantedDexterity);
	}

	private Task Grant(decimal amount) => Grant(amount, amount);
	private async Task Grant(decimal strength, decimal dexterity)
	{
		if (strength <= 0 && dexterity <= 0) return;
		Data data = GetInternalData<Data>();
		var context = new BlockingPlayerChoiceContext();
		decimal before = Owner.GetPower<StrengthPower>()?.Amount ?? 0;
		await PowerCmd.Apply<StrengthPower>(context, Owner, strength, Owner, null);
		data.GrantedStrength += (Owner.GetPower<StrengthPower>()?.Amount ?? 0) - before;
		before = Owner.GetPower<DexterityPower>()?.Amount ?? 0;
		await PowerCmd.Apply<DexterityPower>(context, Owner, dexterity, Owner, null);
		data.GrantedDexterity += (Owner.GetPower<DexterityPower>()?.Amount ?? 0) - before;
		Flash();
	}
}
