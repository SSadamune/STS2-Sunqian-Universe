using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Potions;

/// <summary>
/// 火油泡饭：给予所有敌人灼烧与不熄。
/// </summary>
[RegisterPotion(typeof(SunqianPotionPool), StableEntryStem = "fire_oil_rice")]
public sealed class FireOilRicePotion : ModPotionTemplate
{
	public const decimal BurningStacks = 7m;
	public const decimal UnextinguishedStacks = 2m;

	public override PotionRarity Rarity => PotionRarity.Common;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AllEnemies;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BurningStacks),
		new PowerVar<UnextinguishedPower>(UnextinguishedStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<UnextinguishedPower>(),
	];

	public override PotionAssetProfile AssetProfile => new(
		ImagePath: "res://images/potions/FireOilRice.png",
		OutlinePath: "res://images/potions/FireOilRiceOutline.png");

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState!.HittableEnemies;
		foreach (Creature enemy in enemies)
		{
			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				enemy,
				DynamicVars[nameof(BurningPower)].BaseValue,
				Owner.Creature,
				null);
			await PowerCmd.Apply<UnextinguishedPower>(
				choiceContext,
				enemy,
				DynamicVars[nameof(UnextinguishedPower)].BaseValue,
				Owner.Creature,
				null);
		}
	}
}
