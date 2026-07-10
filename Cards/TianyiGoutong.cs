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
	private const decimal ScryAmount = 3m;
	private const decimal BaseBuffAmount = 2m;
	private const decimal UpgradedBuffAmount = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScryVar(ScryAmount),
		new PowerVar<StrengthPower>(BaseBuffAmount),
		new PowerVar<DexterityPower>(BaseBuffAmount),
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
		await PowerCmd.Apply<TianyiGoutongPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(StrengthPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(StrengthPower)].UpgradeValueBy(UpgradedBuffAmount - BaseBuffAmount);
		DynamicVars[nameof(DexterityPower)].UpgradeValueBy(UpgradedBuffAmount - BaseBuffAmount);
	}
}
