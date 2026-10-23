using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptSunqianPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public int GrantedDexterity;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptSunqianPower.png",
		BigIconPath: "res://images/powers/ScriptSunqianPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(SunqianScript.BaseDexterity),
		new CardsVar(1),
	];

	protected override object InitInternalData() => new Data();

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		int dexterity = cardSource is SunqianScript { IsUpgraded: true }
			? SunqianScript.UpgradedDexterity
			: SunqianScript.BaseDexterity;
		GetInternalData<Data>().GrantedDexterity = dexterity;
		DynamicVars[nameof(DexterityPower)].BaseValue = dexterity;
		await PowerCmd.Apply<DexterityPower>(
			new ThrowingPlayerChoiceContext(),
			Owner,
			dexterity,
			Owner,
			cardSource);
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		int dexterity = GetInternalData<Data>().GrantedDexterity;
		if (dexterity > 0 && oldOwner.GetPower<DexterityPower>() is { } dexterityPower)
		{
			await PowerCmd.ModifyAmount(
				new ThrowingPlayerChoiceContext(),
				dexterityPower,
				-dexterity,
				oldOwner,
				null);
		}

		if (!SuppressScriptEndEffects
			&& oldOwner.Player is { } player
			&& !CombatManager.Instance.IsOverOrEnding)
		{
			SquSfx.Play(SquSfx.SunqianScriptRemovedEvent);
			await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, player);
		}

		await base.AfterRemoved(oldOwner);
	}
}
