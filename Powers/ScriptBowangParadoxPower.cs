using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// 博望悖论剧本：失效时，获得一次性的火种层数，并丢弃手牌中所有能造成灼烧的牌。
/// 火种的具体层数在打出时（<see cref="AfterApplied"/>）根据牌是否已升级快照到内部数据，
/// 失效结算（<see cref="AfterRemoved"/>）时直接读取快照值，不依赖届时的 DynamicVars 状态。
/// </summary>
[RegisterPower]
public sealed class ScriptBowangParadoxPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public decimal TinderStacks;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<TinderPower>(BowangParadoxScript.BaseTinderStacks),
	];

	protected override object InitInternalData() => new Data();

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBowangParadoxPower.png",
		BigIconPath: "res://images/powers/ScriptBowangParadoxPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		decimal stacks = cardSource is BowangParadoxScript { IsUpgraded: true }
			? BowangParadoxScript.UpgradedTinderStacks
			: BowangParadoxScript.BaseTinderStacks;
		GetInternalData<Data>().TinderStacks = stacks;
		DynamicVars[nameof(TinderPower)].BaseValue = stacks;
		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		decimal stacks = GetInternalData<Data>().TinderStacks;
		Player? player = oldOwner.Player;
		if (stacks > 0 && player is not null)
		{
			PlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();

			await PowerCmd.Apply<TinderPower>(
				choiceContext,
				oldOwner,
				stacks,
				oldOwner,
				null);

			List<CardModel> burningCards = PileType.Hand.GetPile(player).Cards
				.Where(SquCardTags.AppliesBurning)
				.ToList();
			if (burningCards.Count > 0)
			{
				await CardCmd.DiscardAndDraw(choiceContext, burningCards, 0);
			}
		}

		await base.AfterRemoved(oldOwner);
	}
}
