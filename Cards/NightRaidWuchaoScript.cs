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
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 夜袭乌巢剧本：将两张附带消耗的星夜打击加入手牌；已解锁时强化剧本，使「打击」名卡牌额外给予更多灼烧。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "night_raid_wuchao_script")]
public sealed class NightRaidWuchaoScript : ScriptCardTemplate<NightRaidWuchaoScriptRecord>
{
	public const int GeneratedStrikeCount = 2;
	public const int BaseBurning = 3;
	public const int UpgradedBurning = 5;
	protected override bool UsesReinforceActivationText => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		// This is the fixed amount stored in the Script power, not Burning applied by this card.
		// A PowerVar<BurningPower> would incorrectly include Burning-given modifiers in its preview.
		new DynamicVar(nameof(BurningPower), BaseBurning),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
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
		SquSfx.PlayRandom(RunState, SquSfx.NightRaidWuchaoPlayEvents);
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
		await UnlockScriptAsync(choiceContext, burningAmount);
	}

	internal override int GetScriptPowerAmountForPreview() =>
		(int)DynamicVars[nameof(BurningPower)].BaseValue;

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}
}
