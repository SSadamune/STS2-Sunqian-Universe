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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 杀意感知：攻击所有敌人后，选择一名盟友；升级前随机、升级后由盟友选择一张攻击牌，
/// 对随机敌人自动打出。
/// 上将军会使这些跨玩家自动打出的攻击牌继承本牌实际消耗的活力加成。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "killing_intent_perception")]
public sealed class KillingIntentPerception : ModCardTemplate
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 13;

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
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException(
				"KillingIntentPerception requires an active combat.");

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(combatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		if (combatState.PlayerCreatures.Count(creature => creature.IsAlive) <= 1
			|| !combatState.HittableEnemies.Any(enemy => enemy.IsAlive))
		{
			return;
		}

		Player? ally = await ChooseAlly();
		if (ally is null || ally.Creature.IsDead)
		{
			return;
		}

		CardModel? attack;
		if (IsUpgraded)
		{
			attack = await ChooseAttack(choiceContext, ally);
		}
		else
		{
			List<CardModel> playableAttacks = GetPlayableAttacks(ally);
			attack = playableAttacks.Count > 0
				? Owner.RunState.Rng.CombatCardSelection.NextItem(playableAttacks)
				: null;
		}

		if (attack is not null)
		{
			await CardCmd.AutoPlay(choiceContext, attack, null);
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

	private async Task<CardModel?> ChooseAttack(
		PlayerChoiceContext choiceContext,
		Player ally)
	{
		if (GetPlayableAttacks(ally).Count == 0)
		{
			return null;
		}

		var prefs = new CardSelectorPrefs(SelectionPrompt, minCount: 0, maxCount: 1)
		{
			RequireManualConfirmation = true,
		};
		return (await CardSelectCmd.FromHand(
			choiceContext,
			ally,
			prefs,
			IsPlayableAttack,
			this)).FirstOrDefault();
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
