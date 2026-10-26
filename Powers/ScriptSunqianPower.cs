using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptSunqianPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptSunqianPower.png",
		BigIconPath: "res://images/powers/ScriptSunqianPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(SunqianScript.BaseDexterity),
	];

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		int dexterity = cardSource is SunqianScript { IsUpgraded: true }
			? SunqianScript.UpgradedDexterity
			: SunqianScript.BaseDexterity;
		DynamicVars[nameof(DexterityPower)].BaseValue = dexterity;
		await PowerCmd.Apply<TempDexPower>(
			ActivationChoiceContext,
			Owner,
			dexterity,
			Owner,
			cardSource);
	}
}
