using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "too_smart_to_be_real")]
public sealed class TooSmartToBeReal : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TooSmartToBeReal.png");

	public TooSmartToBeReal()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.PlayRandom(
			RunState,
			SquSfx.CunningYuanShuWhyPursueEvent,
			SquSfx.CunningYuanShuGreedyGloryEvent);
		await PowerCmd.Apply<TooSmartToBeRealPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars.Cards.BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
