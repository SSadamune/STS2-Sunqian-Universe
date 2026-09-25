using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "sunqian_script")]
[RegisterCharacterStarterCard(typeof(SunqianCharacter), 1)]
public sealed class SunqianScript : ScriptCardTemplate
{
	public const int BaseDexterity = 2;
	public const int UpgradedDexterity = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(BaseDexterity),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SunqianScript.png");

	public SunqianScript()
		: base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<TempDexFromSunqianScriptPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(DexterityPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptSunqianPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(DexterityPower)]
			.UpgradeValueBy(UpgradedDexterity - BaseDexterity);
	}
}
