using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "bowang_paradox_script")]
public sealed class BowangParadoxScript : ScriptCardTemplate<BowangParadoxAdvanceScriptRecord>
{
	protected override bool MergeScriptActivationKeywords => false;
	public const int BaseVigor = 7;
	public const int UpgradedVigor = 9;
	public const int BaseTinderStacks = 7;
	public const int UpgradedTinderStacks = 9;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
		new PowerVar<TinderPower>(BaseTinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalScriptHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.ScriptCollection),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BowangParadoxScript.png");

	public BowangParadoxScript()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.BowangParadoxPlayEvent);
		await ScriptSystem.UnlockOrReinforceAsync<BowangParadoxAdvanceScriptRecord>(
			choiceContext, this, DynamicVars[nameof(VigorPower)].BaseValue, activate: false);
		await ScriptSystem.UnlockOrReinforceAsync<BowangParadoxArsonScriptRecord>(
			choiceContext, this, DynamicVars[nameof(TinderPower)].BaseValue, activate: false);
		await BowangParadoxChoice.OfferAsync(choiceContext, Owner);
	}

	internal override IEnumerable<ScriptRecordCardTemplate> CreateScriptRecordPreviews()
	{
		var advance = (BowangParadoxAdvanceScriptRecord)ModelDb.Card<BowangParadoxAdvanceScriptRecord>().ToMutable();
		var arson = (BowangParadoxArsonScriptRecord)ModelDb.Card<BowangParadoxArsonScriptRecord>().ToMutable();
		advance.PreviewFrom(this);
		arson.PreviewFrom(this);
		return [advance, arson];
	}

	internal override int GetScriptPowerAmountForPreview() =>
		(int)DynamicVars[nameof(VigorPower)].BaseValue;

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
		DynamicVars[nameof(TinderPower)].UpgradeValueBy(UpgradedTinderStacks - BaseTinderStacks);
	}
}
