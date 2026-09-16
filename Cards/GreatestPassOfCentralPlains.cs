using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 中原第一雄关：战斗开始时先于首轮抽牌入手；向任意玩家投掷一瓶药水并给予不受敏捷影响的格挡；
/// 使用后从主卡组永久移除。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "greatest_pass_of_central_plains")]
public sealed class GreatestPassOfCentralPlains : ModCardTemplate
{
	public const decimal BlockAmount = 18m;
	public const decimal UpgradedBlockAmount = 36m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BlockAmount, ValueProp.Unpowered),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		CardKeyword.Exhaust,
	];

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GreatestPassOfCentralPlains.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		CreateAnnotationHoverTip(),
	];

	public GreatestPassOfCentralPlains()
		: base(0, CardType.Attack, CardRarity.Rare, SquTargetTypes.AnyPlayer)
	{
	}

	public override async Task BeforeCombatStart()
	{
		if (Pile is { IsCombatPile: true, Type: not PileType.Hand })
		{
			await CardPileCmd.Add(this, PileType.Hand);
			if (Pile?.Type == PileType.Hand)
			{
				SquSfx.Play(SquSfx.GreatestPassOfCentralPlainsEvent);
			}
		}
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		SquSfx.Play(SquSfx.GreatestPassOfCentralPlainsEvent);

		PotionModel potionProxy = ModelDb.Potion<BlockPotion>().ToMutable();
		potionProxy.Owner = Owner;

		await Hook.BeforePotionUsed(Owner.RunState, CombatState, potionProxy, cardPlay.Target);
		await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay);
		await Hook.AfterPotionUsed(Owner.RunState, CombatState, potionProxy, cardPlay.Target);

		if (DeckVersion is { Pile.Type: PileType.Deck } deckVersion)
		{
			await CardPileCmd.RemoveFromDeck(deckVersion, showPreview: false);
			DeckVersion = null;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlockAmount - BlockAmount);
	}

	private IHoverTip CreateAnnotationHoverTip()
	{
		LocString description = new(
			"cards",
			"SUNQIAN_UNIVERSE_CARD_GREATEST_PASS_OF_CENTRAL_PLAINS.annotation");
		description.Add(DynamicVars.Block);
		return new HoverTip(SquCommonL10n.AnnotationTitle(), description);
	}
}
