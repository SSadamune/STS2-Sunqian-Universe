using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "self_decapitation_ascension")]
public sealed class SelfDecapitationAscension : ModCardTemplate
{
	private const decimal BaseHpLoss = 12m;
	private const decimal UpgradedHpLoss = 8m;
	private const int DrawCount = 1;
	private const decimal IntangibleAmount = 1m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HpLossVar(BaseHpLoss),
		new CardsVar(DrawCount),
		new PowerVar<IntangiblePower>(IntangibleAmount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SelfDecapitationAscension.png");

	public SelfDecapitationAscension()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.Damage(
			choiceContext,
			Owner.Creature,
			DynamicVars.HpLoss.BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			this,
			cardPlay);

		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		await PowerCmd.Apply<IntangiblePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(IntangiblePower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.HpLoss.UpgradeValueBy(UpgradedHpLoss - BaseHpLoss);
	}
}
