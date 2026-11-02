#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "nominal_brothers")]
public sealed class NominalBrothers : ModCardTemplate
{
	private bool IsBrotherMissing => Owner == null ||
		CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId) is not { IsAlive: true };
	protected override bool IsPlayable => !IsBrotherMissing;
	protected override bool ShouldGlowRedInternal => IsBrotherMissing;
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new CompanionDamageVar(CompanionSummons.SecondBrotherId, 13)];
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[new HoverTip(ModelDb.Power<DieForYouPower>(),
			new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SECOND_BROTHER_DIE_FOR_YOU.description").GetFormattedText(), false)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
	public NominalBrothers() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
	{
		var pet = CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId);
		if (pet is not { IsAlive: true } || play.Target == null) return;
		await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
			.Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(ctx);
		if (pet.IsAlive) await PowerCmd.Apply<ActuallyNotClosePower>(ctx, pet, 2, Owner.Creature, this);
	}
	protected override void OnUpgrade() => DynamicVars["CompanionDamage"].UpgradeValueBy(4);
}
