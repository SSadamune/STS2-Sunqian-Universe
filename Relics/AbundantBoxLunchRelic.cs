using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "abundant_box_lunch")]
public sealed class AbundantBoxLunchRelic : BoxLunchRelic
{
	public const int TurnStartEnergy = 1;
	public const int SwitchesPerTurnLimit = 2;

	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override bool IncludeEnergyHoverTip => true;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/AbundantBoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/AbundantBoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/AbundantBoxLunchRelicBig.png");

	internal override int SwitchesPerTurn => SwitchesPerTurnLimit;

	internal override int EnergyCostPerSwitch => TurnStartEnergy;

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Creature.Side
			|| !participants.Contains(Owner.Creature)
			|| Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(TurnStartEnergy, Owner);
	}
}
