using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "guan_di_form")]
public sealed class GuanDiForm : ScriptCardTemplate<GuanYuCivilVer>
{
    public GuanDiForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
    internal override int GetScriptPowerAmountForPreview() => GuanYuCivilVer.BlockPerEnergy;
    internal override IEnumerable<ScriptRecordCardTemplate> CreateScriptRecordPreviews()
    {
        var civil = (GuanYuCivilVer)ModelDb.Card<GuanYuCivilVer>().ToMutable();
        var martial = (GuanYuMartialVer)ModelDb.Card<GuanYuMartialVer>().ToMutable();
        civil.PreviewFrom(this);
        martial.PreviewFrom(this);
        martial.StoredAmount = 1;
        return [civil, martial];
    }

    protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ScriptSystem.UnlockOrReinforceAsync<GuanYuCivilVer>(
            choiceContext, this, GuanYuCivilVer.BlockPerEnergy, activate: false);
        await ScriptSystem.UnlockOrReinforceAsync<GuanYuMartialVer>(
            choiceContext, this, 1, activate: false);
    }
}
