using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "wrap_up")]
[RegisterCharacterStarterCard(typeof(SunqianCharacter), 1)]
public sealed class WrapUp : ModCardTemplate
{
	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WrapUp.png");

	public WrapUp()
		: base(0, CardType.Skill, CardRarity.Basic, TargetType.Self, false)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ScriptSystem.InvalidateScriptsAsync(Owner.Creature);
		await CardPileCmd.Draw(choiceContext, 1m, Owner);
	}

	protected override void OnUpgrade()
	{
		CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
	}
}
