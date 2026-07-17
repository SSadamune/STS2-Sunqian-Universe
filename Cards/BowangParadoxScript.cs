using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "bowang_paradox_script")]
public sealed class BowangParadoxScript : ScriptCardTemplate
{
	public const decimal BaseVigor = 6m;
	public const decimal UpgradedVigor = 9m;
	public const decimal BaseFuelAbundantStacks = 6m;
	public const decimal UpgradedFuelAbundantStacks = 9m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
		new PowerVar<FuelAbundantPower>(BaseFuelAbundantStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<FuelAbundantPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BowangParadoxScript.png");

	public BowangParadoxScript()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		List<CardModel> attackCards = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Type == CardType.Attack)
			.ToList();
		if (attackCards.Count > 0)
		{
			await CardCmd.DiscardAndDraw(choiceContext, attackCards, 0);
		}

		await PowerCmd.Apply<ScriptBowangParadoxPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
		DynamicVars[nameof(FuelAbundantPower)].UpgradeValueBy(UpgradedFuelAbundantStacks - BaseFuelAbundantStacks);
	}
}
