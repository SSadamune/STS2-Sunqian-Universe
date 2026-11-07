#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using Squ.Character;
using Squ.Powers;
using Squ.RunData;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "sovereign_form")]
public sealed class SovereignForm : ModCardTemplate, IRowKeywordsCard
{
    public SovereignForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(0)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [..HoverTipFactory.FromForge().Where(tip => tip is not CardHoverTip { Card: SovereignBlade }),
         SovereignFormPower.CreateStrikePreview()];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/SovereignForm.png");
    protected override void AddExtraArgsToDescription(LocString description)
    {
        DynamicVars.Forge.BaseValue = IsMutable && Owner != null && RunManager.Instance.IsInProgress
            ? RighteousnessRunData.GetSpentAmount(Owner) : 0;
        DynamicVars.Forge.PreviewValue = DynamicVars.Forge.BaseValue;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ForgeCmd.Forge(RighteousnessRunData.GetSpentAmount(Owner), Owner, this);
        await PowerCmd.Apply<SovereignFormPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
