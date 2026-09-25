using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 分布式编剧：正常的回合开始抽牌结束后，选择 <see cref="Amount"/> 张手牌，
/// 将其分别变化为随机剧本牌（触发时点和选牌方式对齐原版 ENTROPY）。
/// </summary>
[RegisterPower]
public sealed class DistributedScreenwriterPower : ModPowerTemplate
{
	public const string BonusDrawCountVarName = "BonusDrawCount";
	public const string HasBonusDrawVarName = "HasBonusDraw";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/DistributedScreenwriterPower.png",
		BigIconPath: "res://images/powers/DistributedScreenwriterPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(BonusDrawCountVarName, 0m),
		new BoolVar(HasBonusDrawVarName),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Transform),
	];

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this
			&& amount > 0m
			&& cardSource is DistributedScreenwriter { IsUpgraded: true })
		{
			DynamicVars[BonusDrawCountVarName].BaseValue++;
			((BoolVar)DynamicVars[HasBonusDrawVarName]).BoolVal = true;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(
		PlayerChoiceContext choiceContext,
		Player player)
	{
		if (player != Owner.Player || Amount <= 0m)
		{
			return;
		}

		Flash();
		int bonusDrawCount = DynamicVars[BonusDrawCountVarName].IntValue;
		if (bonusDrawCount > 0)
		{
			await CardPileCmd.Draw(choiceContext, bonusDrawCount, player);
		}

		CardSelectorPrefs prefs = new(CardSelectorPrefs.TransformSelectionPrompt, Amount);
		List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
			choiceContext,
			player,
			prefs,
			null,
			this)).ToList();

		foreach (CardModel selectedCard in selectedCards)
		{
			CardModel? scriptCard = CreateRandomScriptCard(player);
			if (scriptCard is null)
			{
				break;
			}

			await CardCmd.Transform(selectedCard, scriptCard);
		}
	}

	private static CardModel? CreateRandomScriptCard(Player player) =>
		CardFactory.GetDistinctForCombat(
			player,
			player.Character.CardPool.GetUnlockedCards(
				player.UnlockState,
				player.RunState.CardMultiplayerConstraint)
				.Where(card => card.Tags.Contains(SquCardTags.Script)),
			1,
			player.RunState.Rng.CombatCardGeneration)
			.FirstOrDefault();
}
