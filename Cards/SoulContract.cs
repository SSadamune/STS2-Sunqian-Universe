#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "soul_contract")]
[RegisterCharacterStarterCard(typeof(LiubeiCharacter), 1)]
public sealed class SoulContract : ModCardTemplate
{
    public SoulContract() : base(1, CardType.Power, CardRarity.Basic, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<SharedLifePower>(), HoverTipFactory.FromKeyword(SquKeywords.CompanionSkill)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var options = new[] { CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId }.Select(id =>
        {
            var option = CombatState!.CreateCard<SoulContractRecord>(Owner);
            option.CompanionId = id;
            return (MegaCrit.Sts2.Core.Models.CardModel)option;
        }).ToArray();
        var prefs = new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionPrompt"), 1)
        { Cancelable = false, RequireManualConfirmation = true, PretendCardsCanBePlayed = true, UnpoweredPreviews = true };
        var choice = (await CardSelectCmd.FromSimpleGrid(context, options, Owner, prefs)).OfType<SoulContractRecord>().SingleOrDefault();
        if (choice == null) return;
        await CompanionSkills.UnlockForCompanion<SharedLifePower>(context, Owner, choice.CompanionId);
        await CompanionSkillBook.RecordContract(Owner, choice.CompanionId);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
