#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

/// <summary>Display-only Slight Revision preview for Milk Dragon Emerges.</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "milk_dragon_bowang_script_preview")]
public sealed class MilkDragonBowangScriptPreview : ModCardTemplate
{
    public MilkDragonBowangScriptPreview() : base(1, CardType.Skill, CardRarity.Token,
        TargetType.Self, showInCardLibrary: false) { }
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Script];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.Script, CardKeyword.Exhaust];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BowangParadoxScript.png");
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}
