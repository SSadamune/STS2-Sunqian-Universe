using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "tianyi_goutong")]
public sealed class TianyiGoutong : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScryVar(3m),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public TianyiGoutong()
		: base(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int scryAmount = DynamicVars.Scry().IntValue;

		await TianyiGoutongPower.ExecuteProcess(choiceContext, Owner, scryAmount);

		await PowerCmd.Apply<TianyiGoutongPower>(
			choiceContext,
			Owner.Creature,
			scryAmount,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Scry().UpgradeValueBy(1m);
	}
}
