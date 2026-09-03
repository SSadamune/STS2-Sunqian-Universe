using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 丰盛盒饭：盒饭的欧洛巴斯之触升级。每回合前三次剧本失效获得能量；战斗开始获得剧本：副将。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "abundant_box_lunch")]
public sealed class AbundantBoxLunchRelic : ScriptRelicTemplate, IScriptLiftHandler
{
	public const int EnergyLiftsPerTurn = 3;

	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override bool IncludeEnergyHoverTip => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<ScriptViceGeneralPower>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/AbundantBoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/AbundantBoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/AbundantBoxLunchRelicBig.png");

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature))
		{
			return;
		}

		Status = RelicStatus.Active;

		if (Owner.PlayerCombatState?.TurnNumber > 1)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ScriptViceGeneralPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			1m,
			Owner.Creature,
			null);
	}

	public async Task OnScriptLiftAsync(ScriptLiftContext context)
	{
		if (context.LiftsThisTurn > EnergyLiftsPerTurn)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(1m, Owner);
		if (context.LiftsThisTurn >= EnergyLiftsPerTurn)
		{
			Status = RelicStatus.Normal;
		}
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}
}
