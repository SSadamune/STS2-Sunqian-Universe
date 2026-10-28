using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_civil_ver")]
public sealed class GuanYuCivilVer : ScriptRecordCardTemplate<GuanYuCivilVerPower, GuanDiForm>
{
    public const int BlockPerEnergy = 2;
    public const string FormGroup = "guan_yu_form";
    public override bool CanBeGeneratedInCombat => false;
    public override bool CountsTowardActivationLimit => false;
    public override string MutuallyExclusiveGroup => FormGroup;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.ForEnergy(this),
            HoverTipFactory.Static(StaticHoverTip.Block)];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
}
