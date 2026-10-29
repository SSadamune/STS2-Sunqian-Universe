using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

public abstract class BowangParadoxScriptRecordTemplate<TPower>
    : MaterialReuseScriptRecordCardTemplate<TPower, BowangParadoxScript>
    where TPower : ScriptPowerTemplate
{
    protected override int InitialAmount => 7;
    protected override int AmountPerUpgrade => 2;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/cards/BowangParadoxScript.png");

}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bowang_paradox_advance")]
public sealed class BowangParadoxAdvanceScriptRecord
    : BowangParadoxScriptRecordTemplate<ScriptBowangParadoxAdvancePower>
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.FromKeyword(MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Retain)];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bowang_paradox_arson")]
public sealed class BowangParadoxArsonScriptRecord
    : BowangParadoxScriptRecordTemplate<ScriptBowangParadoxArsonPower>
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
            HoverTipFactory.FromPower<BurningPower>(),
            HoverTipFactory.FromKeyword(MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Retain)];
}
