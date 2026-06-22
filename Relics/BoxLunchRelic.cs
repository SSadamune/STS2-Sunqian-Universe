using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "box_lunch")]
public sealed class BoxLunchRelic : ScriptRelicTemplate
{
	private bool _gainedEnergyThisTurn;

	public override RelicRarity Rarity => RelicRarity.Common;

	protected override bool IncludeEnergyHoverTip => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<ScriptSentryPower>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/BoxLunchRelic.png",
		BigIconPath: "res://images/relics/BoxLunchRelic.png");

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature))
		{
			return;
		}

		_gainedEnergyThisTurn = false;

		if (Owner.PlayerCombatState?.TurnNumber > 1)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ScriptSentryPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			1m,
			Owner.Creature,
			null);
	}

	/// <summary>
	/// 遗物效果：解除剧本时获得能量（与剧本能力本身无关，移除剧本不会取消此效果）。
	/// </summary>
	public async Task OnScriptLiftedAsync(PlayerChoiceContext choiceContext)
	{
		if (_gainedEnergyThisTurn)
		{
			return;
		}

		_gainedEnergyThisTurn = true;
		Flash();
		await PlayerCmd.GainEnergy(1m, Owner);
	}
}
