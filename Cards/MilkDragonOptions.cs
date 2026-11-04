#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

public abstract class MilkDragonOption : ModCardTemplate
{
    internal abstract bool IsArson { get; }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", BowangParadoxScript.BaseVigor)];
    public MilkDragonOption() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BowangParadoxScript.png");
    internal void SetAmount(decimal amount) => DynamicVars["Amount"].BaseValue = amount;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => IsArson
        ? [..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(), HoverTipFactory.FromPower<BurningPower>(), HoverTipFactory.FromKeyword(CardKeyword.Retain)]
        : [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.FromKeyword(CardKeyword.Retain)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "milk_dragon_advance")]
public sealed class MilkDragonAdvance : MilkDragonOption { internal override bool IsArson => false; }
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "milk_dragon_arson")]
public sealed class MilkDragonArson : MilkDragonOption { internal override bool IsArson => true; }
