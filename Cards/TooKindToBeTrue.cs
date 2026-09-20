using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 长厚似伪：本场战斗中将一张攻击牌变化为防御+（升级后为究极防御）。
/// 你每有一种负面状态，带 <see cref="CardTag.Defend"/> 的牌额外获得格挡。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "too_kind_to_be_true")]
public sealed class TooKindToBeTrue : ModCardTemplate
{
	public const decimal ExtraBlockPerDebuff = 3m;
	public const decimal UpgradedExtraBlockPerDebuff = 4m;
	private const string ExtraBlockKey = "ExtraBlock";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ExtraBlockKey, ExtraBlockPerDebuff),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			yield return HoverTipFactory.Static(StaticHoverTip.Block);
			if (IsUpgraded)
			{
				yield return HoverTipFactory.FromCard<UltimateDefend>();
				yield break;
			}

			// 图鉴规范卡不可变，访问 Owner 会 AssertMutable；无主人时回退到龙套防御。
			Player? owner = IsMutable ? Owner : null;
			yield return HoverTipFactory.FromCard(
				GetCharacterBasicDefend(owner) ?? ModelDb.Card<DefendLongtao>(),
				upgrade: true);
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TooKindToBeTrue.png");

	public TooKindToBeTrue()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.TooKindToBeTrueEvent);
		CardSelectorPrefs prefs = new(CardSelectorPrefs.TransformSelectionPrompt, 1);
		CardModel? attack = (await CardSelectCmd.FromHand(
			choiceContext,
			Owner,
			prefs,
			IsTransformableAttack,
			this)).FirstOrDefault();

		if (attack is { IsTransformable: true })
		{
			await TransformAttack(attack);
		}

		await PowerCmd.Apply<TooKindToBeTruePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[ExtraBlockKey].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[ExtraBlockKey].UpgradeValueBy(UpgradedExtraBlockPerDebuff - ExtraBlockPerDebuff);
	}

	private async Task TransformAttack(CardModel original)
	{
		if (IsUpgraded)
		{
			await CardCmd.TransformTo<UltimateDefend>(original);
			return;
		}

		if (original.CombatState is not { } combatState)
		{
			return;
		}

		CardModel canonical = GetCharacterBasicDefend(original.Owner) ?? ModelDb.Card<DefendLongtao>();
		CardModel replacement = combatState.CreateCard(canonical, original.Owner);
		replacement.UpgradeInternal();
		replacement.FinalizeUpgradeInternal();
		await CardCmd.Transform(original, replacement);
	}

	private static bool IsTransformableAttack(CardModel card) =>
		card.Type == CardType.Attack && card.IsTransformable;

	private static CardModel? GetCharacterBasicDefend(Player? player)
	{
		if (player?.Character.CardPool is not { } pool)
		{
			return null;
		}

		return pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
			.FirstOrDefault(card => card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Defend));
	}
}
