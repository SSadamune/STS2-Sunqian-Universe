using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_martial_ver")]
public sealed class GuanYuMartialVer : MaterialReuseScriptRecordCardTemplate<GuanYuMartialVerPower, GuanDiForm>
{
    public override bool CanBeGeneratedInCombat => false;
    protected override int InitialAmount => 2;
    protected override int AmountPerUpgrade => 2;
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    protected override IEnumerable<IHoverTip> DirectOnlyHoverTips =>
    [
        new HoverTip(SquCommonL10n.AnnotationTitle(), new LocString("cards", Id.Entry + ".annotation")),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
}
