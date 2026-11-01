using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_civil_ver")]
public sealed class GuanYuCivilVer : MaterialReuseScriptRecordCardTemplate<GuanYuCivilVerPower, GuanDiForm>
{
    public const string FormGroup = "guan_yu_form";
    public override bool CanBeGeneratedInCombat => false;
    protected override int InitialAmount => 2;
    protected override int AmountPerUpgrade => 2;
    public override string MutuallyExclusiveGroup => FormGroup;
    protected override IEnumerable<IHoverTip> DirectOnlyHoverTips =>
    [
        new HoverTip(SquCommonL10n.AnnotationTitle(), new LocString("cards", Id.Entry + ".annotation")),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
}
