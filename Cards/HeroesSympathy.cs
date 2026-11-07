using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 英雄相惜：自己获得虚弱，本回合每次攻击前让所有活着的敌人获得虚弱。
/// 基础牌消耗，升级后不再消耗；能力可叠层。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "heroes_sympathy")]
public sealed class HeroesSympathy : ModCardTemplate
{
	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<HeroesSympathyPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/PrisonerPlaceholder.png");

	public HeroesSympathy()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<WeakPower>(
			choiceContext,
			Owner.Creature,
			1,
			Owner.Creature,
			this);
		await PowerCmd.Apply<HeroesSympathyPower>(
			choiceContext,
			Owner.Creature,
			1,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
