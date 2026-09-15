using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "guan_di_form")]
public sealed class GuanDiForm : ModCardTemplate
{
	private const string PowerVarName = "Power";

	public const int BaseAmountPerEnergy = 2;

	public const int UpgradedAmountPerEnergy = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(PowerVarName, BaseAmountPerEnergy),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanDiForm()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.WhoAreYouEvent);
		await PowerCmd.Apply<GuanDiFormPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[PowerVarName].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[PowerVarName].UpgradeValueBy(UpgradedAmountPerEnergy - BaseAmountPerEnergy);
	}
}
