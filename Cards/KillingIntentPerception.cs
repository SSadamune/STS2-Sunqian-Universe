using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 杀意感知：攻击指定敌人，再让一名（升级后所有）盟友各自选择至多一张攻击牌对该敌人打出。
/// 随机目标攻击必须包含此目标，并继承上将军的活力加成。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "killing_intent_perception")]
public sealed class KillingIntentPerception : ModCardTemplate
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 13;
	public const int BaseAttackCount = 1;
	public const int UpgradedAttackCount = 1;

	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_KILLING_INTENT_PERCEPTION.selectionScreenPrompt");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.SupremeGeneral,
	];

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KillingIntentPerception.png");

	public KillingIntentPerception()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.KillingIntentPerceptionPlayEvent);

		ICombatState combatState = CombatState
			?? throw new InvalidOperationException(
				"KillingIntentPerception requires an active combat.");

		ArgumentNullException.ThrowIfNull(cardPlay.Target);
		Creature target = cardPlay.Target;
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(target)
			.WithHitVfxNode(target => NScratchVfx.Create(target, goingRight: true))
			.Execute(choiceContext);

		if (combatState.PlayerCreatures.Count(creature => creature.IsAlive) <= 1
			|| !target.IsAlive)
		{
			return;
		}

		List<Player> allies;
		if (IsUpgraded)
		{
			allies = combatState.PlayerCreatures.Where(creature => creature.IsAlive && creature.Player != Owner)
				.Select(creature => creature.Player!).ToList();
		}
		else
		{
			Player? ally = await ChooseAlly();
			allies = ally is null ? [] : [ally];
		}
		// Keep nested cross-player choices inside the current resolution.
		var allyChoiceContext = new BlockingPlayerChoiceContext();
		foreach (Player ally in allies)
		{
			if (!target.IsAlive) break;
			if (ally.Creature.IsDead) continue;
			List<CardModel> attacks = await ChooseAttacks(allyChoiceContext, ally);
			foreach (CardModel attack in attacks)
			{
				if (!target.IsAlive) break;
				if (attack.Pile?.Type != PileType.Hand) continue;
				using (SquRandomEnemyTargeting.RequireTarget(attack, target))
					await CardCmd.AutoPlay(allyChoiceContext, attack, target);
			}
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private static List<CardModel> GetPlayableAttacks(Player player) =>
		PileType.Hand.GetPile(player).Cards
			.Where(IsPlayableAttack)
			.ToList();

	private static bool IsPlayableAttack(CardModel card) =>
		card.Type == CardType.Attack && CanAutoPlayForFree(card);

	private async Task<List<CardModel>> ChooseAttacks(
		PlayerChoiceContext choiceContext,
		Player ally)
	{
		if (GetPlayableAttacks(ally).Count == 0)
		{
			return [];
		}

		int maxCount = IsUpgraded ? UpgradedAttackCount : BaseAttackCount;
		var prefs = new CardSelectorPrefs(SelectionPrompt, minCount: 0, maxCount)
		{
			RequireManualConfirmation = true,
		};
		return (await CardSelectCmd.FromHand(
			choiceContext,
			ally,
			prefs,
			IsPlayableAttack,
			this)).ToList();
	}

	private async Task<Player?> ChooseAlly()
	{
		uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer
			.ReserveChoiceId(Owner);
		if (LocalContext.IsMe(Owner))
		{
			Vector2 startPosition = NCombatRoom.Instance?
				.GetCreatureNode(Owner.Creature)?.VfxSpawnPosition ?? Vector2.Zero;
			NTargetManager targetManager = NTargetManager.Instance;
			targetManager.StartTargeting(
				TargetType.AnyAlly,
				startPosition,
				TargetMode.ClickMouseToTarget,
				null,
				null);
			Player? selected = NodeToPlayer(await targetManager.SelectionFinished());
			RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
				Owner,
				choiceId,
				PlayerChoiceResult.FromPlayerId(selected?.NetId));
			return selected;
		}

		ulong? selectedPlayerId = (await RunManager.Instance.PlayerChoiceSynchronizer
			.WaitForRemoteChoice(Owner, choiceId)).AsPlayerId();
		return selectedPlayerId.HasValue
			? Owner.RunState.GetPlayer(selectedPlayerId.Value)
			: null;
	}

	private static Player? NodeToPlayer(Node? node) => node switch
	{
		NCreature creatureNode => creatureNode.Entity.Player,
		NMultiplayerPlayerState playerState => playerState.Player,
		_ => null,
	};

	private static bool CanAutoPlayForFree(CardModel card)
	{
		card.CanPlay(out UnplayableReason reason, out _);
		const UnplayableReason ignoredResourceCosts =
			UnplayableReason.EnergyCostTooHigh | UnplayableReason.StarCostTooHigh;
		return (reason & ~ignoredResourceCosts) == UnplayableReason.None;
	}
}
