using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>博望悖论激活时的活力选项，只出现在二选一界面。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "bowang_paradox_vigor_choice")]
public sealed class BowangParadoxVigorChoice : ModCardTemplate
{
	public override bool CanBeGeneratedInCombat => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BowangParadoxScript.BaseVigor),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BowangParadoxScript.png");

	public BowangParadoxVigorChoice()
		: base(-1, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override void OnUpgrade() =>
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(
			BowangParadoxScript.UpgradedVigor - BowangParadoxScript.BaseVigor);

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;
}

/// <summary>博望悖论激活时的火种选项，只出现在二选一界面。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "bowang_paradox_tinder_choice")]
public sealed class BowangParadoxTinderChoice : ModCardTemplate
{
	public override bool CanBeGeneratedInCombat => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<TinderPower>(BowangParadoxScript.BaseTinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BowangParadoxScript.png");

	public BowangParadoxTinderChoice()
		: base(-1, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override void OnUpgrade() =>
		DynamicVars[nameof(TinderPower)].UpgradeValueBy(
			BowangParadoxScript.UpgradedTinderStacks - BowangParadoxScript.BaseTinderStacks);

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;
}
