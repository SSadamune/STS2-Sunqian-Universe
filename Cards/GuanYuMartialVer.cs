using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>关帝形态的武关羽选项。只出现在二选一界面，不会进入牌组。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_martial_ver")]
public sealed class GuanYuMartialVer : ModCardTemplate
{
	public const int BaseVigorPerEnergy = 1;

	public const int UpgradedVigorPerEnergy = 2;

	public override bool CanBeGeneratedInCombat => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigorPerEnergy),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanYuMartialVer()
		: base(-1, CardType.Power, CardRarity.Token, TargetType.Self)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigorPerEnergy - BaseVigorPerEnergy);
	}
}
