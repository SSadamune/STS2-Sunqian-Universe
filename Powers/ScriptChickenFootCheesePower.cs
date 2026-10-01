using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Squ.Audio;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「剧本：鸡脚芝士」：剧本存续期间，基础 <see cref="CardTag.Strike"/> 牌改为随机两名敌人目标。
/// </summary>
[RegisterPower]
public sealed class ScriptChickenFootCheesePower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptChickenFootCheesePower.png",
		BigIconPath: "res://images/powers/ScriptChickenFootCheesePowerBig.png");

	public const int RedirectRandomEnemyCount = 2;
	public const int RedirectHitCountPerTarget = 2;

	public static bool ShouldRedirectBasicStrike(CardModel card)
	{
		if (!card.IsMutable)
		{
			return false;
		}

		if (card.Owner?.Creature?.GetPower<ScriptChickenFootCheesePower>() is null)
		{
			return false;
		}

		return card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike);
	}

	public static bool ShouldDisplayRedirectedBasicStrike(CardModel card) =>
		ShouldRedirectBasicStrike(card)
		&& card.Pile?.Type is PileType.Hand or PileType.Play;

	public void TriggerForRedirectedStrike()
	{
		Flash();
		SquSfx.PlayRandom(CombatState?.RunState, SquSfx.ChickenFootCheeseScriptEvents);
	}

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		SquStrikeRedirectPatches.EnsureApplied();
		RefreshOnTable(Owner);
		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		await base.AfterRemoved(oldOwner);
		RefreshOnTable(oldOwner);
	}

	private static void RefreshOnTable(Creature owner)
	{
		if (owner.Player?.PlayerCombatState is not { } combatState)
		{
			return;
		}

		foreach (CardModel card in combatState.AllCards.Where(card =>
			card.Rarity == CardRarity.Basic
			&& card.Tags.Contains(CardTag.Strike)
			&& card.Pile?.Type is PileType.Hand or PileType.Play))
		{
			NCard.FindOnTable(card)?.UpdateVisuals(card.Pile!.Type, CardPreviewMode.Normal);
		}
	}
}
