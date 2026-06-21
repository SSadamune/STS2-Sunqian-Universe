using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HujinHuyuan : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<HujinHuyuanPower>(HujinHuyuanPower.BaseMaxDexterity),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<HujinHuyuanPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/HujinHuyuan.png");

	public HujinHuyuan()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int maxDexterity = (int)DynamicVars[nameof(HujinHuyuanPower)].BaseValue;
		int dexterity = HujinHuyuanPower.RollTemporaryDexterity(Owner, maxDexterity);

		await HujinHuyuanPower.ApplyTemporaryDexterityAsync(
			choiceContext,
			Owner.Creature,
			Owner.Creature,
			this,
			dexterity);

		await PowerCmd.Apply<HujinHuyuanPower>(
			choiceContext,
			Owner.Creature,
			maxDexterity,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(HujinHuyuanPower)].UpgradeValueBy(
			HujinHuyuanPower.UpgradedMaxDexterity - HujinHuyuanPower.BaseMaxDexterity);
	}
}
