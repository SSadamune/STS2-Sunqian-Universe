using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "buddy_longtaodi")]
public sealed class BuddyLongtaodi : ModCardTemplate
{
	public const int BaseDraw = 1;
	public const int UpgradedDraw = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseDraw),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BuddyLongtaodi.png");

	public BuddyLongtaodi()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<BuddyLongtaodiPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars.Cards.BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedDraw - BaseDraw);
	}
}
