using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "sunqian_script")]
[RegisterCharacterStarterCard(typeof(SunqianCharacter), 1)]
public sealed class SunqianScript : ScriptCardTemplate<SunqianScriptRecord>
{
	public const int BaseDexterity = 2;
	public const int UpgradedDexterity = 3;
	public const int BaseDraw = 1;
	public const int UpgradedDraw = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(BaseDexterity),
		new CardsVar(BaseDraw),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SunqianScript.png");

	public SunqianScript()
		: base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardDrawPlayRateTracker.TryGetStats(
			Owner,
			this,
			CardDrawPlayRateTracker.MaxStoredCombats,
			includeCurrentCombat: false,
			out int playFromHandCount,
			out int handEntryCount);
		bool frequentlyPlayed = handEntryCount > 0
			&& (long)playFromHandCount * 2 >= handEntryCount;
		SquSfx.Play(frequentlyPlayed
			? SquSfx.SunqianScriptFrequentAppearanceEvent
			: SquSfx.SunqianScriptRareTrueFormEvent);

		await CardPileCmd.Draw(
			choiceContext,
			(int)DynamicVars.Cards.BaseValue,
			Owner);
		await UnlockScriptAsync(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(DexterityPower)]
			.UpgradeValueBy(UpgradedDexterity - BaseDexterity);
		DynamicVars.Cards.UpgradeValueBy(UpgradedDraw - BaseDraw);
	}
}
