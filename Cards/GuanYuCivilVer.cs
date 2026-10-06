using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>关帝形态的文关羽选项。只出现在二选一界面，不会进入牌组。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_civil_ver")]
public sealed class GuanYuCivilVer : ModCardTemplate
{
	public const int BlockPerSkill = 2;

	public override bool CanBeGeneratedInCombat => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BlockPerSkill, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<SunqianScript>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanYuCivilVer()
		: base(-1, CardType.Power, CardRarity.Token, TargetType.Self)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;
}
