#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>铁索连舟：令目标后续受到的灼烧伤害扩散至所有敌人。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "iron_chain_boats")]
public sealed class IronChainBoats : ModCardTemplate
{
	public const int ChainStacks = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IronChainPower>(ChainStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(SquKeywords.Environmental),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/IronChainBoats.png");

	public IronChainBoats()
		: base(1, CardType.Power, CardRarity.Token, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.IronChainBoatsPlayEvent);
		await PowerCmd.Apply<IronChainPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(IronChainPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
