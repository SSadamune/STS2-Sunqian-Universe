using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Script;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 可叠层剧本能力：与其它剧本（含其它可叠层剧本）之间仍正常替换并触发剧本失效；
/// 仅与同名剧本能力之间改为层数叠加，不触发剧本失效。
/// </summary>
public abstract class StackableScriptPowerTemplate : ScriptPowerTemplate
{
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerInstanceType InstanceType => PowerInstanceType.None;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		new HoverTip(
			SquCommonL10n.StackableScriptTitle(),
			SquCommonL10n.StackableScriptAnnotation()),
	];

	public override async Task BeforeApplied(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		System.Type powerType = GetType();
		foreach (ScriptPowerTemplate active in target.Powers.OfType<ScriptPowerTemplate>().ToList())
		{
			if (active.GetType() != powerType)
			{
				await ScriptSystem.RemoveScriptPowerAsync(active);
			}
		}
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		OnStackedFrom(cardSource);
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (amount > 0m)
		{
			OnStackedFrom(cardSource);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 首次获得或叠层后，根据打出剧本牌的快照合并内部状态。
	/// </summary>
	protected abstract void OnStackedFrom(CardModel? cardSource);

	/// <summary>
	/// 合并升级状态：只要曾打出过升级剧本牌，则保留升级效果。
	/// </summary>
	protected static void MergeUpgradedFlag(ref bool upgraded, CardModel? cardSource)
	{
		if (cardSource is { IsUpgraded: true })
		{
			upgraded = true;
		}
	}

	/// <summary>
	/// 优先保留升级剧本牌作为效果来源卡。
	/// </summary>
	protected static CardModel? PreferUpgradedSourceCard(CardModel? current, CardModel? incoming)
	{
		if (incoming is null)
		{
			return current;
		}

		if (incoming.IsUpgraded)
		{
			return incoming;
		}

		return current ?? incoming;
	}
}
