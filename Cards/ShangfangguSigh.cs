using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "shangfanggu_sigh")]
public sealed class ShangfangguSigh : ModCardTemplate
{
	public const int BaseBonusPercent = 100;
	public const int UpgradedBonusPercent = 150;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<GoodFirePower>(BaseBonusPercent),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<GoodFirePower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ShangfangguSigh.png");

	public ShangfangguSigh()
		: base(1, CardType.Power, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.YilingFineFireEvent);
		await PowerCmd.Apply<GoodFirePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(GoodFirePower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(GoodFirePower)].UpgradeValueBy(UpgradedBonusPercent - BaseBonusPercent);
	}
}
