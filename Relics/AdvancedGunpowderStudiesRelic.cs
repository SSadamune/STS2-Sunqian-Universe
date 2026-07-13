using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 《高级火药学》：每场战斗开始时获得 5 层燃料充足；玩家造成灼烧后清零概率降至 0%。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "advanced_gunpowder_studies")]
public sealed class AdvancedGunpowderStudiesRelic : ModRelicTemplate
{
	public const decimal CombatStartFuelAbundant = 5m;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<FuelAbundantPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/AdvancedGunpowderStudiesRelic.png",
		IconOutlinePath: "res://images/relics/AdvancedGunpowderStudiesRelicOutline.png",
		BigIconPath: "res://images/relics/AdvancedGunpowderStudiesRelicBig.png");

	public override async Task BeforeCombatStart()
	{
		Flash();
		await PowerCmd.Apply<FuelAbundantPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			CombatStartFuelAbundant,
			Owner.Creature,
			null);
	}

	internal static void TryApplyZeroClearChance(Creature? applier, BurningPower burning)
	{
		if (applier?.Player is null)
		{
			return;
		}

		foreach (RelicModel relic in applier.Player.Relics)
		{
			if (relic is AdvancedGunpowderStudiesRelic owned)
			{
				owned.Flash();
				burning.ReduceClearChanceTo(0f);
				return;
			}
		}
	}
}
