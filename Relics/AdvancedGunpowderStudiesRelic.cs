using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 《高级火药学》：每回合开始时获得 2 层火种；玩家造成灼烧后清零概率降至 0%。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "advanced_gunpowder_studies")]
public sealed class AdvancedGunpowderStudiesRelic : ModRelicTemplate
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<TinderPower>(2m),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/AdvancedGunpowderStudiesRelic.png",
		IconOutlinePath: "res://images/relics/AdvancedGunpowderStudiesRelicOutline.png",
		BigIconPath: "res://images/relics/AdvancedGunpowderStudiesRelicBig.png");

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<TinderPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			DynamicVars[nameof(TinderPower)].BaseValue,
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
