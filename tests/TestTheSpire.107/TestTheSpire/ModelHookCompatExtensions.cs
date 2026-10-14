using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TestTheSpire;

public static class ModelHookCompatExtensions
{
	public static Task BeforeTurnEnd(this AbstractModel model, PlayerChoiceContext choiceContext, CombatSide side)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		PowerModel val = (PowerModel)(object)((model is PowerModel) ? model : null);
		IReadOnlyList<Creature> readOnlyList;
		if (val != null)
		{
			Creature owner = val.Owner;
			if (owner != null)
			{
				ICombatState combatState = owner.CombatState;
				if (combatState != null)
				{
					readOnlyList = combatState.GetCreaturesOnSide(side);
					goto IL_0030;
				}
			}
		}
		IReadOnlyList<Creature> readOnlyList2 = Array.Empty<Creature>();
		readOnlyList = readOnlyList2;
		goto IL_0030;
		IL_0030:
		IReadOnlyList<Creature> readOnlyList3 = readOnlyList;
		return model.BeforeSideTurnEnd(choiceContext, side, (IEnumerable<Creature>)readOnlyList3);
	}

	public static Task AfterTurnEnd(this AbstractModel model, PlayerChoiceContext choiceContext, CombatSide side)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		PowerModel val = (PowerModel)(object)((model is PowerModel) ? model : null);
		IReadOnlyList<Creature> readOnlyList;
		if (val != null)
		{
			Creature owner = val.Owner;
			if (owner != null)
			{
				ICombatState combatState = owner.CombatState;
				if (combatState != null)
				{
					readOnlyList = combatState.GetCreaturesOnSide(side);
					goto IL_0030;
				}
			}
		}
		IReadOnlyList<Creature> readOnlyList2 = Array.Empty<Creature>();
		readOnlyList = readOnlyList2;
		goto IL_0030;
		IL_0030:
		IReadOnlyList<Creature> readOnlyList3 = readOnlyList;
		return model.AfterSideTurnEnd(choiceContext, side, (IEnumerable<Creature>)readOnlyList3);
	}
}
