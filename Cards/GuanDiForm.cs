using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "guan_di_form")]
public sealed class GuanDiForm : ModCardTemplate
{
	private const int BaseDexterity = 1;
	private const int UpgradedDexterity = 2;
	private const int BaseStrength = 2;
	private const int UpgradedStrength = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(BaseDexterity),
		new PowerVar<StrengthPower>(BaseStrength),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanDiForm()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<GuanDiFormPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(DexterityPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(DexterityPower)].UpgradeValueBy(UpgradedDexterity - BaseDexterity);
		DynamicVars[nameof(StrengthPower)].UpgradeValueBy(UpgradedStrength - BaseStrength);
	}
}
