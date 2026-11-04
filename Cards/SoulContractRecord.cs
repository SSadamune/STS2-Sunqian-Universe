#nullable enable
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

/// <summary>The player-owned contract action is independent of companion skill slots.</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "soul_contract_record")]
public sealed class SoulContractRecord : ModCardTemplate
{
    [SavedProperty] public string CompanionId { get; set; } = CompanionSummons.SecondBrotherId;
    public SoulContractRecord() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, showInCardLibrary: false) { }
    public override int MaxUpgradeLevel => 0;
    public override string Title => ModelDb.Card<SoulContract>().Title + "·" + CompanionSummons.GetName(CompanionId).GetFormattedText();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<LiubeiCardPool>();
    public override CardAssetProfile AssetProfile => new(PortraitPath: CompanionSummons.GetPortraitPath(CompanionId));
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Companion", CompanionSummons.GetName(CompanionId));
    protected override bool ShouldGlowGoldInternal => Owner != null && CompanionSkillBook.CanActivate(Owner, CompanionId);
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
