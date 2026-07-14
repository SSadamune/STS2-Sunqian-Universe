using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// [gold]预见[/gold]数量的卡牌动态变量。卡牌在 <c>CanonicalVars</c> 中声明
/// <c>new ScryVar(n)</c>，即可在文案中以 <c>{Scry}</c> 引用，并让预览反映 <see cref="IModifyScryAmount"/> 的修改。
/// </summary>
public sealed class ScryVar : DynamicVar
{
	public const string VarName = "Scry";

	public ScryVar(decimal baseValue) : base(VarName, baseValue)
	{
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
		bool runGlobalHooks)
	{
		var amount = IntValue;
		if (runGlobalHooks)
			amount = ScryHook.ModifyScryAmount(card.Owner, amount, out _);
		PreviewValue = amount;
	}
}

/// <summary>
/// 便捷访问卡牌上的 <see cref="ScryVar"/>。
/// </summary>
public static class ScryDynamicVarExtensions
{
	public static ScryVar Scry(this DynamicVarSet vars) => (ScryVar)vars[ScryVar.VarName];
}
