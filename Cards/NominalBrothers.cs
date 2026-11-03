#nullable enable
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
	private IEnumerable<string> LivingBrothers => new[] { CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId }
		.Where(id => Owner != null && CompanionSummons.GetCompanion(Owner, id) is { IsAlive: true });
	private bool IsBrotherMissing => !LivingBrothers.Any();
	protected override bool IsPlayable => !IsBrotherMissing;
	protected override bool ShouldGlowRedInternal => IsBrotherMissing;
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new CompanionDamageVar(CompanionSummons.SecondBrotherId, 13, card => ((NominalBrothers)card).LivingBrothers.FirstOrDefault() ?? CompanionSummons.SecondBrotherId)];
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[new HoverTip(ModelDb.Power<DieForYouPower>(),
			new LocString("powers", "SUNQIAN_UNIVERSE_POWER_COMPANION_DIE_FOR_YOU.description").GetFormattedText(), false)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
	public NominalBrothers() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
	{
		var ids = LivingBrothers.ToArray();
		if (ids.Length == 0 || play.Target == null) return;
		string selectedId = ids[0];
		if (ids.Length > 1)
		{
			var options = ids.Select(id =>
			{
				var option = CombatState!.CreateCard<BrotherAttackChoice>(Owner);
				option.Configure(id, DynamicVars["CompanionDamage"].BaseValue);
				return (CardModel)option;
			}).ToList();
			var prefs = new CardSelectorPrefs(new LocString("cards", "SUNQIAN_UNIVERSE_CARD_NOMINAL_BROTHERS.selectionPrompt"), 1)
			{
				Cancelable = false, RequireManualConfirmation = true,
				PretendCardsCanBePlayed = true, UnpoweredPreviews = true,
			};
			var selection = (await CardSelectCmd.FromSimpleGrid(ctx, options, Owner, prefs))
				.OfType<BrotherAttackChoice>().SingleOrDefault();
			if (selection == null) return;
			selectedId = selection.CompanionId;
		}
		var pet = CompanionSummons.GetCompanion(Owner, selectedId);
		if (pet is not { IsAlive: true } || play.Target == null) return;
		await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
			.Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(ctx);
		if (pet.IsAlive) await PowerCmd.Apply<ActuallyNotClosePower>(ctx, pet, 2, Owner.Creature, this);
	}
	protected override void OnUpgrade() => DynamicVars["CompanionDamage"].UpgradeValueBy(4);
}
