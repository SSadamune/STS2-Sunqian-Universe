#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "companion_skill_record")]
public sealed class CompanionSkillRecord : ModCardTemplate
{
    [SavedProperty] public string CompanionId { get; set; } = CompanionSummons.SecondBrotherId;
    [SavedProperty] public string Skill { get; set; } = "SharedLife";
    public CompanionSkillRecord() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false) { }
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [new HoverTip(new LocString("cards", "SUNQIAN_UNIVERSE_CARD_COMPANION_SKILL_RECORD.title"),
            new LocString("powers", "SUNQIAN_UNIVERSE_COMPANION_SKILL.rules"))];
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<LiubeiCardPool>();
    public override CardAssetProfile AssetProfile => new(PortraitPath: CompanionSummons.GetPortraitPath(CompanionId));
    public override string Title => Power.Title.GetFormattedText() + "·" + CompanionSummons.GetName(CompanionId).GetFormattedText();
    internal PowerModel Power => Skill switch
    {
        "MakeHoles" => ModelDb.Power<MakeHolesPower>(),
        "HardToRestrain" => ModelDb.Power<HardToRestrainPower>(),
        "SpareTheYoungAndOld" => ModelDb.Power<SpareTheYoungAndOldPower>(),
        _ => ModelDb.Power<SharedLifePower>()
    };
    protected override bool ShouldGlowGoldInternal => Owner != null && CompanionSummons.GetCompanion(Owner, CompanionId)
        ?.GetPower(Power.Id) is CompanionSkillPower { IsActive: true };
    protected override void AddExtraArgsToDescription(LocString description)
    {
        var text = Skill == "SharedLife"
            ? SharedLifePower.CreateDescription(CompanionId, Owner != null ? SharedLifeSystem.GetTotal(Owner, CompanionId) : 0)
            : Power.Description;
        SquKeywords.AddNestedLoc(description, "SkillDescription", text);
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
