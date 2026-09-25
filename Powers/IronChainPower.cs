#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>铁索连环：目标受到灼烧伤害后，按层数将实际伤害量作为环境伤害传给其余敌人。</summary>
[RegisterPower]
public sealed class IronChainPower : ModPowerTemplate
{
	private static readonly PowerModel ChainsOfBindingPowerTemplate =
		ModelDb.Power<ChainsOfBindingPower>();

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: ChainsOfBindingPowerTemplate.PackedIconPath,
		BigIconPath: ChainsOfBindingPowerTemplate.ResolvedBigIconPath);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(SquKeywords.Environmental),
	];

	public async Task AfterBurningDamage(int damageDealt)
	{
		if (Amount <= 0 || damageDealt <= 0)
		{
			return;
		}

		Flash();
		SquSfx.Play(SquSfx.IronChainBoatsTriggerEvent);
		List<Creature> otherEnemies = Owner.CombatState!.HittableEnemies
			.Where(enemy => enemy != Owner && enemy.IsAlive)
			.ToList();
		if (otherEnemies.Count > 0)
		{
			for (int repeat = 0; repeat < Amount; repeat++)
			{
				foreach (Creature enemy in otherEnemies.Where(enemy => enemy.IsAlive))
				{
					await CreatureCmd.Damage(
						new ThrowingPlayerChoiceContext(),
						enemy,
						damageDealt,
						ValueProp.Unpowered,
						null,
						null);
				}
			}
		}
	}
}
