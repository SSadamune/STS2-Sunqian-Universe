using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TestTheSpire;

public sealed class InlineEncounterModel : EncounterModel
{
	private List<CombatTestBattleBuilder.MonsterFactory> _monsterFactories;

	private RoomType _roomType;

	public override RoomType RoomType
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _roomType;
		}
	}

	public override IEnumerable<MonsterModel> AllPossibleMonsters => _monsterFactories.Select((CombatTestBattleBuilder.MonsterFactory factory) => factory.Canonical);

	internal void Configure(RoomType roomType, IEnumerable<CombatTestBattleBuilder.MonsterFactory> monsterFactories)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		((AbstractModel)this).AssertMutable();
		_roomType = roomType;
		_monsterFactories = monsterFactories.ToList();
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return _monsterFactories.Select((CombatTestBattleBuilder.MonsterFactory factory) => ((MonsterModel, string))(factory.Create(), null)).ToArray();
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		_monsterFactories = _monsterFactories.ToList();
	}

	public InlineEncounterModel()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		_monsterFactories = new List<CombatTestBattleBuilder.MonsterFactory>();
		_roomType = (RoomType)1;
	}
}
