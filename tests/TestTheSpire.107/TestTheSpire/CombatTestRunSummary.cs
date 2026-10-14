using System.Collections.Generic;
using System.Linq;

namespace TestTheSpire;

internal sealed record CombatTestRunSummary(int Total, int Passed, int Failed, int Skipped, IReadOnlyList<CombatTestRunner.TestResult> Results)
{
	public static CombatTestRunSummary FromResults(IReadOnlyList<CombatTestRunner.TestResult> results)
	{
		return new CombatTestRunSummary(results.Count, results.Count((CombatTestRunner.TestResult result) => result.Outcome == CombatTestRunner.TestOutcome.Passed), results.Count((CombatTestRunner.TestResult result) => result.Outcome == CombatTestRunner.TestOutcome.Failed), results.Count((CombatTestRunner.TestResult result) => result.Outcome == CombatTestRunner.TestOutcome.Skipped), results);
	}
}
