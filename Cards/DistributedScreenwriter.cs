using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "distributed_screenwriter")]
public sealed class DistributedScreenwriter : ModCardTemplate
{
	public const int BaseCost = 2;
	public const int PowerAmount = 1;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Transform),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/DistributedScreenwriter.png");

	public DistributedScreenwriter()
		: base(BaseCost, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DistributedScreenwriterPower? power = await PowerCmd.Apply<DistributedScreenwriterPower>(
			choiceContext,
			Owner.Creature,
			PowerAmount,
			Owner.Creature,
			this);

		if (IsUpgraded)
		{
			power?.RecordUpgradedPlay();
		}
	}

}
