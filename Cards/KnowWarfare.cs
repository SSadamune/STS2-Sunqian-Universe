#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "know_warfare")]
public sealed class KnowWarfare : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<BurningPower>(4), new BlockVar(4, ValueProp.Move),
		 new PowerVar<TemporaryHealthPower>(4), new PowerVar<VigorPower>(4)];
	public override bool GainsBlock => true;
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/RelocateCapital.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<BurningPower>(), HoverTipFactory.FromPower<TemporaryHealthPower>(), HoverTipFactory.FromPower<VigorPower>()];
	public KnowWarfare() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
	{
		await PowerCmd.Apply<BurningPower>(ctx, Owner.Creature, DynamicVars[nameof(BurningPower)].BaseValue, Owner.Creature, this);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
		await PowerCmd.Apply<TemporaryHealthPower>(ctx, Owner.Creature, DynamicVars[nameof(TemporaryHealthPower)].BaseValue, Owner.Creature, this);
		await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars[nameof(VigorPower)].BaseValue, Owner.Creature, this);
	}
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2);
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(2);
	}
}
