#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "can_i_stop_running")]
public sealed class CanIStopRunning : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StopRunningPower>(1)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/RelocateCapital.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<StopRunningPower>(), HoverTipFactory.FromPower<TemporaryHealthPower>()];
	public CanIStopRunning() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
		await PowerCmd.Apply<StopRunningPower>(context, Owner.Creature,
			DynamicVars[nameof(StopRunningPower)].BaseValue, Owner.Creature, this);
	protected override void OnUpgrade() => DynamicVars[nameof(StopRunningPower)].UpgradeValueBy(1);
}
