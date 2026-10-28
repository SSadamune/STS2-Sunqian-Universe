using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_martial_ver")]
public sealed class GuanYuMartialVer : ScriptRecordCardTemplate<GuanYuMartialVerPower, GuanDiForm>
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CountsTowardActivationLimit => false;
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), ..NewsanguoDrunkenMightEffect.HoverTips()];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        NewsanguoDrunkenMightEffect.AddOrText(description);
    }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
}
