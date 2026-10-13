using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "guan_di_form")]
public sealed class GuanDiForm : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		CreateFormHoverTip<GuanYuCivilVer>(),
		CreateFormHoverTip<GuanYuMartialVer>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanDiForm()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	/// <summary>
	/// 升级预览保留“刚升级”状态，让两张选项的变化内容显示为绿色。
	/// 手牌悬停和二选一会完成升级定稿，因此保持普通颜色。
	/// </summary>
	private IHoverTip CreateFormHoverTip<T>()
		where T : CardModel
	{
		if (UpgradePreviewType == CardUpgradePreviewType.None)
		{
			return HoverTipFactory.FromCard<T>(IsUpgraded);
		}

		CardModel card = (CardModel)ModelDb.Card<T>().MutableClone();
		card.UpgradeInternal();
		return new CardHoverTip(card);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await GuanDiFormChoice.OfferAsync(
			choiceContext,
			Owner,
			this,
			cardPlay.Resources.EnergySpent,
			IsUpgraded,
			canSkip: false);
	}
}
