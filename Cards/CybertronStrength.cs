using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "cybertron_strength")]
public sealed class CybertronStrength : ModCardTemplate
{
	public const decimal BaseBlock = 7m;
	public const decimal UpgradedBlock = 10m;
	public const int BurningPerExtraBlock = 10;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BaseBlock, ValueProp.Move),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CybertronStrength.png");

	protected override bool ShouldGlowGoldInternal =>
		GetTotalEnemyBurning(CombatState) >= BurningPerExtraBlock;

	public CybertronStrength()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		if (combatState is null)
		{
			return;
		}

		SquSfx.Play(SquSfx.LuXunCybertronEvent);
		int blockGains = 1 + GetTotalEnemyBurning(combatState) / BurningPerExtraBlock;
		for (int i = 0; i < blockGains; i++)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
	}

	private static int GetTotalEnemyBurning(ICombatState? combatState) =>
		combatState?.HittableEnemies
			.Where(creature => creature.IsAlive)
			.Sum(creature => creature.GetPowerAmount<BurningPower>()) ?? 0;
}
