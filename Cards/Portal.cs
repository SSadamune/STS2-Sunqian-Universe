using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "portal")]
public sealed class Portal : ModCardTemplate
{
	private const decimal ScryAmount = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScryVar(ScryAmount),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Portal.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	public Portal()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ScryCmd.Execute(choiceContext, this);

		CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
		if (drawn is not { Type: CardType.Attack })
		{
			return;
		}

		// 「对随机目标打出」：AnyEnemy 目标传 null 时会自动随机选敌。
		await CardCmd.AutoPlay(choiceContext, drawn, null);

		// 升级后额外抽的这张牌不再走上面的攻击判定，故此处只是单纯抽牌。
		if (IsUpgraded)
		{
			await CardPileCmd.Draw(choiceContext, Owner);
		}
	}
}
