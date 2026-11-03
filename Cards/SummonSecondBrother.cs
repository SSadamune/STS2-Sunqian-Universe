#nullable enable
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "summon_second_brother")]
[RegisterCharacterStarterCard(typeof(LiubeiCharacter), 1)]
public sealed class SummonSecondBrother : BrotherSummonCard, ISlightRevisionSource
{
    protected override string CompanionId => CompanionSummons.SecondBrotherId;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.SlightRevision];
    CardModel ISlightRevisionSource.SlightRevisionTarget => ModelDb.Card<SummonThirdBrother>();
    bool ISlightRevisionSource.SlightRevisionTargetUpgraded => IsUpgraded;
    public SummonSecondBrother() : base(CardRarity.Basic) { }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [..base.AdditionalHoverTips, ..(this.Capability<SlightRevisionCapability>() is null
            ? SlightRevisionSystem.GetHoverTips(this, ModelDb.Card<SummonThirdBrother>(), IsUpgraded) : [])];
    protected override void AddExtraSelfHarmArgs(LocString description)
    {
        base.AddExtraSelfHarmArgs(description);
        if (this.Capability<SlightRevisionCapability>() is null)
            SlightRevisionSystem.AddDescription(description, ModelDb.Card<SummonThirdBrother>(), IsUpgraded);
        else description.Add("SlightRevisionText", "");
    }
}
