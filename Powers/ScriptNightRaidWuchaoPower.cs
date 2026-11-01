using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本：夜袭乌巢。<see cref="CardTag.Strike"/> 卡牌攻击后，额外给予目标 <see cref="Amount"/> 层灼烧。
/// 素材复用：初始 3 层，每次剧本升级增加 2 层。
/// </summary>
[RegisterPower]
public sealed class ScriptNightRaidWuchaoPower : ScriptPowerTemplate
{
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptNightRaidWuchaoPower.png",
		BigIconPath: "res://images/powers/ScriptNightRaidWuchaoPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(NightRaidWuchaoScript.BaseBurning),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		SyncDisplayVars();
		return Task.CompletedTask;
	}

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (Owner.IsDead || Amount <= 0m || dealer != Owner
			|| cardSource is null || cardSource.Owner?.Creature != Owner
			|| !cardSource.Tags.Contains(CardTag.Strike)
			|| result.TotalDamage <= 0 || !result.Receiver.IsAlive)
		{
			return;
		}

		Flash();
		// The Script applies this Burning independently of the card's own effects.
		// In particular, a Burning-tagged Strike must not pass its Tinder bonus here.
		await PowerCmd.Apply<BurningPower>(choiceContext, result.Receiver, Amount, Owner, cardSource: null);
	}

	private void SyncDisplayVars() =>
		DynamicVars[nameof(BurningPower)].BaseValue = Amount;
}
