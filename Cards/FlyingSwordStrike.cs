#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "flying_sword_strike")]
public sealed class FlyingSwordStrike : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new CardsVar(1)];
	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/FlyingSwordStrike.png");
	public FlyingSwordStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target!)
			.WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
		await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
	}
	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
		Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (cardSource != this || !props.IsPoweredAttack() || Owner == null) return 1;
		int kinds = Owner.Creature.Powers.Where(power => power.Type == PowerType.Debuff).Select(power => power.Id).Distinct().Count();
		return 1 + (IsUpgraded ? kinds : System.Math.Min(1, kinds));
	}
	protected override void OnUpgrade() { }
}
