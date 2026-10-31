using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
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
		new CardsVar(SunqianScript.BaseDraw),
	];

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		SquSfx.Play(SquSfx.SunqianScriptActivatedEvent);

		int dexterity = cardSource is SunqianScript { IsUpgraded: true }
			? SunqianScript.UpgradedDexterity
			: SunqianScript.BaseDexterity;
		DynamicVars[nameof(DexterityPower)].BaseValue = dexterity;
		DynamicVars.Cards.BaseValue = cardSource is { IsUpgraded: true } ? SunqianScript.UpgradedDraw : SunqianScript.BaseDraw;
		await PowerCmd.Apply<TempDexPower>(
			ActivationChoiceContext,
			Owner,
			dexterity,
			Owner,
			cardSource);
	}
	public override async Task AfterRemoved(Creature oldOwner)
	{
		if (!SuppressScriptEndEffects && oldOwner.Player is { } player && !oldOwner.IsDead)
			await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), DynamicVars.Cards.IntValue, player);
		await base.AfterRemoved(oldOwner);
	}
}
