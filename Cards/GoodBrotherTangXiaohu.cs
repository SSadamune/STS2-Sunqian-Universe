using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "good_brother_tang_xiaohu")]
public sealed class GoodBrotherTangXiaohu : ModCardTemplate
{
	public const int BaseBlock = 5;
	public const int UpgradedBlock = 8;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BaseBlock, ValueProp.Move),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GoodBrotherTangXiaohu.png");

	public GoodBrotherTangXiaohu()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<GoodBrotherTangXiaohuPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars.Block.BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
	}
}
