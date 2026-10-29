using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_martial_ver")]
public sealed class GuanYuMartialVer : ScriptRecordCardTemplate<GuanYuMartialVerPower, GuanDiForm>
{
    public override bool CanBeGeneratedInCombat => false;
    internal override bool HasUpgradedVersion => true;
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ScriptAmount", 2)];
    protected override void SyncDisplayVars() => DynamicVars["ScriptAmount"].BaseValue = StoredAmount;
    protected override void OnUpgrade() => DynamicVars["ScriptAmount"].UpgradeValueBy(1);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.ForEnergy(this)];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
}
