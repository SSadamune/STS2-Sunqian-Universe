using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using Squ.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "guan_di_form")]
public sealed class GuanDiForm : ScriptCardTemplate<GuanYuCivilVer>
{
    protected override HashSet<CardTag> CanonicalTags => [];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];
    protected override IEnumerable<IHoverTip> AdditionalScriptHoverTips =>
    [
        HoverTipFactory.FromKeyword(SquKeywords.Script),
        HoverTipFactory.FromKeyword(SquKeywords.ScriptCollection),
    ];
    public GuanDiForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
    internal override int GetScriptPowerAmountForPreview() => IsUpgraded ? 4 : 2;
    internal override IEnumerable<ScriptRecordCardTemplate> CreateScriptRecordPreviews()
    {
        var civil = (GuanYuCivilVer)ModelDb.Card<GuanYuCivilVer>().ToMutable();
        var martial = (GuanYuMartialVer)ModelDb.Card<GuanYuMartialVer>().ToMutable();
        civil.PreviewFrom(this);
        martial.PreviewFrom(this);
        return [civil, martial];
    }

    protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var civil = await ScriptSystem.PrepareScriptUpdateAsync<GuanYuCivilVer>(
            this, GetScriptPowerAmountForPreview());
        var martial = await ScriptSystem.PrepareScriptUpdateAsync<GuanYuMartialVer>(
            this, GetScriptPowerAmountForPreview());
        await PowerCmd.Apply<GuanDiFormPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        await ScriptSystem.ChooseActiveScriptsAsync(choiceContext, Owner, cancelable: false,
            requireManualSelection: true, beforeActivation: async () =>
            {
                await civil.CommitAsync();
                await martial.CommitAsync();
            });
    }
}
