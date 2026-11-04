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
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(6)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, DynamicVars.Summon),
     CompanionSummonKeywords.NamedHoverTip(CompanionSummons.ThirdBrotherId, DynamicVars.Summon),
     new HoverTip(new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SHARED_LIFE_POWER.title"), SharedLifePower.CreateDescription())];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var options = new[] { CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId }.Select(id =>
        {
            var option = CombatState!.CreateCard<CompanionSummonChoice>(Owner);
            option.Configure(id, DynamicVars.Summon.BaseValue);
            return (MegaCrit.Sts2.Core.Models.CardModel)option;
        }).ToArray();
        var prefs = new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionPrompt"), 1)
        { Cancelable = false, RequireManualConfirmation = true, PretendCardsCanBePlayed = true, UnpoweredPreviews = true };
        var choice = (await CardSelectCmd.FromSimpleGrid(context, options, Owner, prefs)).OfType<CompanionSummonChoice>().SingleOrDefault();
        if (choice == null) return;
        await CompanionSkills.UnlockForCompanion<SharedLifePower>(context, Owner, choice.CompanionId);
        await CompanionSummons.SummonSpecific(context, Owner, choice.CompanionId, DynamicVars.Summon.BaseValue, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
