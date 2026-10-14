namespace TestTheSpire;

public sealed class CombatTestOptions
{
	public string LogPrefix { get; init; } = "TestTheSpire";

	public string PrimaryArg { get; init; } = "sts2-test";

	public string? LegacyArg { get; init; } = "sts2-test-demo";

	public ZeroActionTestBehavior ZeroActionBehavior { get; init; }
}
