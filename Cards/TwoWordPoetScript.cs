using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "two_word_poet_script")]
public sealed class TwoWordPoetScript : ScriptCardTemplate<TwoWordPoetScriptRecord>
{
	public const int CanonicalDamage = 2;
	public const int CanonicalHits = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, ValueProp.Move),
		new RepeatVar(CanonicalHits),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	public override TargetType TargetType => TargetType.Self;

	public TwoWordPoetScript()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.TwoWordWineAndSongEvent);
		int spentVigor = await SquVigorSnapshot.SpendAll(choiceContext, Owner.Creature, this);
		int spentDrunkenMight = await NewsanguoDrunkenMightEffect.SpendForAttack(
			choiceContext,
			Owner.Creature,
			this);

		await UnlockScriptAsync(
			choiceContext,
			CanonicalDamage,
			record => record.AddConsumedVigor(this, spentVigor + spentDrunkenMight));
	}

	internal override int GetScriptPowerAmountForPreview() => CanonicalDamage;
}
