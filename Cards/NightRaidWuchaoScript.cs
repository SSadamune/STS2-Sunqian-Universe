using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 夜袭乌巢剧本：将两张附带消耗的星夜打击加入手牌；可叠层剧本使「打击」名卡牌额外给予灼烧。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "night_raid_wuchao_script")]
public sealed class NightRaidWuchaoScript : ScriptCardTemplate
{
	public const int GeneratedStrikeCount = 2;
	public const int BaseBurning = 3;
	public const int UpgradedBurning = 5;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BaseBurning),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<StarryNightStrike>(IsUpgraded),
		HoverTipFactory.FromKeyword(SquKeywords.StackableScript),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NightRaidWuchaoScript.png");

	public NightRaidWuchaoScript()
		: base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player player = Owner;
		ICombatState combatState = player.Creature.CombatState
			?? throw new InvalidOperationException("NightRaidWuchaoScript requires an active combat.");

		for (int i = 0; i < GeneratedStrikeCount; i++)
		{
			var strike = GeneratedCombatCards.CreateInCombat<StarryNightStrike>(
				combatState,
				player,
				IsUpgraded);
			strike.AddKeyword(CardKeyword.Exhaust);
			await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, player);
		}

		decimal burningAmount = DynamicVars[nameof(BurningPower)].BaseValue;
		await PowerCmd.Apply<ScriptNightRaidWuchaoPower>(
			choiceContext,
			player.Creature,
			burningAmount,
			player.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}
}
