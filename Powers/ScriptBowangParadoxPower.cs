using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>博望悖论剧本：激活时在活力弃攻和火种弃火之间选择一项。</summary>
[RegisterPower]
public sealed class ScriptBowangParadoxPower : ScriptPowerTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BowangParadoxScript.BaseVigor),
		new PowerVar<TinderPower>(BowangParadoxScript.BaseTinderStacks),
	];

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBowangParadoxPower.png",
		BigIconPath: "res://images/powers/ScriptBowangParadoxPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
	];

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		bool upgraded = cardSource is BowangParadoxScript { IsUpgraded: true };
		decimal tinder = upgraded
			? BowangParadoxScript.UpgradedTinderStacks
			: BowangParadoxScript.BaseTinderStacks;
		DynamicVars[nameof(TinderPower)].BaseValue = tinder;
		int vigor = upgraded
			? BowangParadoxScript.UpgradedVigor
			: BowangParadoxScript.BaseVigor;
		DynamicVars[nameof(VigorPower)].BaseValue = vigor;
		if (Owner.Player is { } player)
		{
			await BowangParadoxChoice.OfferAsync(
				ActivationChoiceContext,
				player,
				cardSource,
				upgraded);
		}
	}
}
