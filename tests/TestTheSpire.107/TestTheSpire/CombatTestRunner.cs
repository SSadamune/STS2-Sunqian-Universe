using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using Xunit;

namespace TestTheSpire;

internal sealed class CombatTestRunner
{
	private sealed record DiscoveredTestCase(Type SuiteType, MethodInfo Method, CombatTestBattleDefinition BattleDefinition, string UniqueName, string DisplayName, object?[] Arguments, string? SkipReason, IReadOnlyList<ulong> NetworkLocalNetIds, bool ExpectNetworkChecksumMismatch = false, bool CompareNetworkChecksumContextsOnly = false);

	private sealed record NetworkChecksumCapture(ulong LocalNetId, IReadOnlyList<RecordedChecksum> Checksums);

	private sealed record RecordedChecksum(uint Id, uint Checksum, string Context);

	internal sealed record TestResult(string DisplayName, TestOutcome Outcome, TimeSpan Elapsed, string? SkipReason, string? Error)
	{
		public static TestResult Passed(string displayName, TimeSpan elapsed)
		{
			return new TestResult(displayName, TestOutcome.Passed, elapsed, null, null);
		}

		public static TestResult Failed(string displayName, TimeSpan elapsed, Exception error)
		{
			return new TestResult(displayName, TestOutcome.Failed, elapsed, null, error.ToString());
		}

		public static TestResult Skipped(string displayName, string? skipReason)
		{
			return new TestResult(displayName, TestOutcome.Skipped, TimeSpan.Zero, skipReason, null);
		}
	}

	internal enum TestOutcome
	{
		Passed,
		Failed,
		Skipped
	}

	private readonly NGame _game;

	public CombatTestRunner(NGame game)
	{
		_game = game;
	}

	public async Task<CombatTestRunSummary> RunAsync(Assembly assembly, string? nameFilter, bool listOnly)
	{
		List<DiscoveredTestCase> list = DiscoverTests(assembly, nameFilter);
		Log.Info($"[{CombatTestBootstrap.LogPrefix}] START mode={(listOnly ? "list" : "run")} total={list.Count}{FormatFilter(nameFilter)}", 2);
		if (listOnly)
		{
			foreach (DiscoveredTestCase item in list)
			{
				Log.Info("[" + CombatTestBootstrap.LogPrefix + "] " + item.DisplayName, 2);
			}
			return new CombatTestRunSummary(list.Count, 0, 0, 0, Array.Empty<TestResult>());
		}
		List<TestResult> results = new List<TestResult>();
		foreach (DiscoveredTestCase item2 in list)
		{
			if (!string.IsNullOrWhiteSpace(item2.SkipReason))
			{
				Log.Info($"[{CombatTestBootstrap.LogPrefix}] SKIP {item2.DisplayName}: {item2.SkipReason}", 2);
				results.Add(TestResult.Skipped(item2.DisplayName, item2.SkipReason));
				continue;
			}
			TestResult testResult = await RunSingleAsync(item2);
			results.Add(testResult);
			Log.Info($"[{CombatTestBootstrap.LogPrefix}] {testResult.Outcome.ToString().ToUpperInvariant()} {testResult.DisplayName} ({testResult.Elapsed.TotalMilliseconds:F0} ms)", 2);
			if (testResult.Error != null)
			{
				Log.Error("[" + CombatTestBootstrap.LogPrefix + "] " + testResult.Error, 2);
			}
		}
		return CombatTestRunSummary.FromResults(results);
	}

	private static string FormatFilter(string? nameFilter)
	{
		if (!string.IsNullOrWhiteSpace(nameFilter))
		{
			return " filter=\"" + nameFilter + "\"";
		}
		return string.Empty;
	}

	private async Task<TestResult> RunSingleAsync(DiscoveredTestCase testCase)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			if (testCase.NetworkLocalNetIds.Count <= 0)
			{
				await RunSinglePassAsync(testCase, testCase.BattleDefinition, captureChecksums: false);
			}
			else
			{
				await RunNetworkChecksumCaseAsync(testCase);
			}
			stopwatch.Stop();
			return TestResult.Passed(testCase.DisplayName, stopwatch.Elapsed);
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			return TestResult.Failed(testCase.DisplayName, stopwatch.Elapsed, Unwrap(ex));
		}
	}

	private async Task<IReadOnlyList<RecordedChecksum>> RunSinglePassAsync(DiscoveredTestCase testCase, CombatTestBattleDefinition battleDefinition, bool captureChecksums)
	{
		CombatTestContext context = null;
		CombatTestSuite suite = null;
		bool suiteDisposed = false;
		bool contextReset = false;
		bool zeroActionHandled = false;
		Action<NetChecksumData, string, NetFullCombatState> recordChecksum = null;
		List<RecordedChecksum> checksums = new List<RecordedChecksum>();
		try
		{
			suite = CreateSuite(testCase);
			string seed = battleDefinition.Seed ?? testCase.UniqueName;
			context = await CombatTestContext.CreateAsync(_game, battleDefinition, seed);
			context.BeginActionExecutionTracking();
			suite.AttachContext(context);
			if (captureChecksums)
			{
				context.EnableChecksumTracking();
				recordChecksum = delegate(NetChecksumData data, string checksumContext, NetFullCombatState _)
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_000c: Unknown result type (might be due to invalid IL or missing references)
					checksums.Add(new RecordedChecksum(data.id, data.checksum, checksumContext));
				};
				RunManager.Instance.ChecksumTracker.ChecksumGenerated += recordChecksum;
			}
			try
			{
				await suite.InitializeInternalAsync();
				await InvokeTestMethodAsync(testCase, suite);
			}
			finally
			{
				if (recordChecksum != null)
				{
					RunManager.Instance.ChecksumTracker.ChecksumGenerated -= recordChecksum;
				}
			}
			zeroActionHandled = true;
			await EnsureCombatActionWasExecutedAsync(testCase, context);
			context.EndActionExecutionTracking();
			await suite.DisposeInternalAsync();
			suiteDisposed = true;
			await context.ResetAsync();
			contextReset = true;
			return checksums;
		}
		catch (Exception ex)
		{
			Exception actual = Unwrap(ex);
			if (!zeroActionHandled)
			{
				CombatTestContext combatTestContext = context;
				if (combatTestContext != null && combatTestContext.ExecutedActionCount == 0)
				{
					try
					{
						await EnsureCombatActionWasExecutedAsync(testCase, context, actual);
					}
					catch (Exception ex2)
					{
						actual = new AggregateException(actual, Unwrap(ex2));
					}
				}
			}
			try
			{
				if (suite != null && !suiteDisposed)
				{
					await suite.DisposeInternalAsync();
				}
			}
			catch (Exception ex3)
			{
				actual = new AggregateException(actual, Unwrap(ex3));
			}
			try
			{
				if (context != null && !contextReset)
				{
					await context.ResetAsync();
				}
			}
			catch (Exception ex4)
			{
				actual = new AggregateException(actual, Unwrap(ex4));
			}
			throw actual;
		}
	}

	private static async Task EnsureCombatActionWasExecutedAsync(DiscoveredTestCase testCase, CombatTestContext context, Exception? originalError = null)
	{
		if (context.ExecutedActionCount <= 0)
		{
			if (CombatTestBootstrap.ZeroActionBehavior == ZeroActionTestBehavior.EnqueueCleanupNoOp)
			{
				await context.EnsureActionExecutedForCleanupAsync(testCase.DisplayName);
			}
			else
			{
				TerminateZeroActionTest(testCase, originalError);
			}
		}
	}

	private static void TerminateZeroActionTest(DiscoveredTestCase testCase, Exception? originalError)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(62, 1, stringBuilder);
		handler.AppendLiteral("TERMINATE ");
		handler.AppendFormatted(testCase.DisplayName);
		handler.AppendLiteral(": completed without executing any combat GameAction.");
		StringBuilder stringBuilder2 = stringBuilder.AppendLine(ref handler).Append("Zero-action combat tests can crash STS2 headless cleanup; execute at least one action before the test returns.");
		if (originalError != null)
		{
			stringBuilder2.AppendLine().AppendLine("Original error before cleanup:").Append(originalError);
		}
		CombatTestBootstrap.TerminateWithError(stringBuilder2.ToString());
	}

	private async Task RunNetworkChecksumCaseAsync(DiscoveredTestCase testCase)
	{
		if (testCase.NetworkLocalNetIds.Count < 2)
		{
			throw new InvalidOperationException(testCase.DisplayName + " uses NetworkChecksumFact but configured fewer than two local net IDs.");
		}
		List<NetworkChecksumCapture> captures = new List<NetworkChecksumCapture>();
		foreach (ulong localNetId in testCase.NetworkLocalNetIds)
		{
			CombatTestBattleDefinition battleDefinition = testCase.BattleDefinition.WithLocalNetId(localNetId);
			captures.Add(new NetworkChecksumCapture(localNetId, await RunSinglePassAsync(testCase, battleDefinition, captureChecksums: true)));
		}
		string text = FindNetworkChecksumMismatch(testCase, captures);
		if (testCase.ExpectNetworkChecksumMismatch)
		{
			if (text == null)
			{
				throw new InvalidOperationException(testCase.DisplayName + " was expected to reproduce a checksum mismatch, but all captures matched.");
			}
			Log.Info("[" + CombatTestBootstrap.LogPrefix + "] Expected checksum mismatch reproduced: " + text.Split('\n')[0], 2);
		}
		else if (text != null)
		{
			throw new InvalidOperationException(text);
		}
	}

	private static string? FindNetworkChecksumMismatch(DiscoveredTestCase testCase, IReadOnlyList<NetworkChecksumCapture> captures)
	{
		NetworkChecksumCapture networkChecksumCapture = captures[0];
		if (networkChecksumCapture.Checksums.Count == 0)
		{
			return $"{testCase.DisplayName} produced no checksums for local net ID {networkChecksumCapture.LocalNetId}.";
		}
		foreach (NetworkChecksumCapture item in captures.Skip(1))
		{
			if (item.Checksums.Count == 0)
			{
				return $"{testCase.DisplayName} produced no checksums for local net ID {item.LocalNetId}.";
			}
			if (item.Checksums.Count != networkChecksumCapture.Checksums.Count)
			{
				return BuildChecksumCountMismatchMessage(testCase, networkChecksumCapture, item);
			}
			for (int i = 0; i < networkChecksumCapture.Checksums.Count; i++)
			{
				RecordedChecksum recordedChecksum = networkChecksumCapture.Checksums[i];
				RecordedChecksum recordedChecksum2 = item.Checksums[i];
				if (recordedChecksum.Id != recordedChecksum2.Id || NormalizeChecksumContext(recordedChecksum.Context) != NormalizeChecksumContext(recordedChecksum2.Context) || (!testCase.CompareNetworkChecksumContextsOnly && recordedChecksum.Checksum != recordedChecksum2.Checksum))
				{
					return BuildChecksumMismatchMessage(testCase, networkChecksumCapture, item, i);
				}
			}
		}
		return null;
	}

	private static CombatTestSuite CreateSuite(DiscoveredTestCase testCase)
	{
		return (Activator.CreateInstance(testCase.SuiteType) as CombatTestSuite) ?? throw new InvalidOperationException("Could not create test suite " + testCase.SuiteType.FullName + ". Ensure it has a parameterless constructor.");
	}

	private static async Task InvokeTestMethodAsync(DiscoveredTestCase testCase, CombatTestSuite suite)
	{
		object obj = testCase.Method.Invoke(suite, testCase.Arguments);
		if (obj != null)
		{
			if (obj is Task task)
			{
				await task;
				return;
			}
			if (obj is ValueTask valueTask)
			{
				await valueTask;
				return;
			}
		}
		else if (testCase.Method.ReturnType == typeof(void))
		{
			return;
		}
		throw new InvalidOperationException(testCase.Method.DeclaringType?.FullName + "." + testCase.Method.Name + " must return void, Task, or ValueTask.");
	}

	private static List<DiscoveredTestCase> DiscoverTests(Assembly assembly, string? nameFilter)
	{
		List<DiscoveredTestCase> list = new List<DiscoveredTestCase>();
		foreach (Type item in (from type in assembly.GetTypes()
			where !type.IsAbstract && typeof(CombatTestSuite).IsAssignableFrom(type)
			select type).OrderBy<Type, string>((Type type) => type.FullName, StringComparer.Ordinal))
		{
			CombatTestBattleDefinition battleDefinition = CreateSuiteForDiscovery(item).BuildBattleDefinition();
			foreach (MethodInfo item2 in item.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy<MethodInfo, string>((MethodInfo method) => method.Name, StringComparer.Ordinal))
			{
				TheoryAttribute customAttribute = item2.GetCustomAttribute<TheoryAttribute>();
				NetworkChecksumFactAttribute customAttribute2 = item2.GetCustomAttribute<NetworkChecksumFactAttribute>();
				FactAttribute customAttribute3 = item2.GetCustomAttribute<FactAttribute>();
				if (customAttribute == null && customAttribute3 == null)
				{
					continue;
				}
				if (customAttribute != null)
				{
					InlineDataAttribute[] array = item2.GetCustomAttributes<InlineDataAttribute>().ToArray();
					if (array.Length == 0)
					{
						list.Add(BuildCase(item, item2, customAttribute, battleDefinition, Array.Empty<object>(), "Theory has no InlineData."));
						continue;
					}
					for (int num = 0; num < array.Length; num++)
					{
						object[] data = array[num].Data;
						string displayName = BuildDisplayName(item, item2, customAttribute, data);
						list.Add(new DiscoveredTestCase(item, item2, battleDefinition, $"{item.FullName}.{item2.Name}[{num}]", displayName, data, customAttribute.Skip, Array.Empty<ulong>()));
					}
				}
				else if (customAttribute2 != null)
				{
					list.Add(BuildNetworkChecksumCase(item, item2, customAttribute2, battleDefinition, customAttribute2.Skip));
				}
				else if (customAttribute3 != null)
				{
					list.Add(BuildCase(item, item2, customAttribute3, battleDefinition, Array.Empty<object>(), customAttribute3.Skip));
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(nameFilter))
		{
			list = list.Where((DiscoveredTestCase test) => test.DisplayName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return list;
	}

	private static CombatTestSuite CreateSuiteForDiscovery(Type suiteType)
	{
		return (Activator.CreateInstance(suiteType) as CombatTestSuite) ?? throw new InvalidOperationException("Could not create test suite " + suiteType.FullName + " during discovery. Ensure it has a parameterless constructor.");
	}

	private static DiscoveredTestCase BuildCase(Type suiteType, MethodInfo method, FactAttribute attribute, CombatTestBattleDefinition battleDefinition, object?[] arguments, string? skipReason)
	{
		return new DiscoveredTestCase(suiteType, method, battleDefinition, suiteType.FullName + "." + method.Name, BuildDisplayName(suiteType, method, attribute, arguments), arguments, skipReason, Array.Empty<ulong>());
	}

	private static DiscoveredTestCase BuildNetworkChecksumCase(Type suiteType, MethodInfo method, NetworkChecksumFactAttribute attribute, CombatTestBattleDefinition battleDefinition, string? skipReason)
	{
		ulong[] networkLocalNetIds = attribute.LocalNetIds.Select(delegate(int netId)
		{
			if (netId <= 0)
			{
				throw new InvalidOperationException($"NetworkChecksumFact local net IDs must be positive. Found {netId}.");
			}
			return (ulong)netId;
		}).ToArray();
		return new DiscoveredTestCase(suiteType, method, battleDefinition, suiteType.FullName + "." + method.Name, BuildDisplayName(suiteType, method, attribute, Array.Empty<object>()), Array.Empty<object>(), skipReason, networkLocalNetIds, attribute.ExpectMismatch, attribute.CompareContextsOnly);
	}

	private static string BuildDisplayName(Type suiteType, MethodInfo method, FactAttribute attribute, IReadOnlyList<object?> arguments)
	{
		if (!string.IsNullOrWhiteSpace(attribute.DisplayName))
		{
			return attribute.DisplayName;
		}
		if (arguments.Count == 0)
		{
			return suiteType.Name + "." + method.Name;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(suiteType.Name);
		stringBuilder.Append('.');
		stringBuilder.Append(method.Name);
		stringBuilder.Append('(');
		stringBuilder.Append(string.Join(", ", arguments.Select(FormatArgument)));
		stringBuilder.Append(')');
		return stringBuilder.ToString();
	}

	private static string FormatArgument(object? argument)
	{
		if (argument != null)
		{
			if (argument is string text)
			{
				return "\"" + text + "\"";
			}
			return argument.ToString() ?? argument.GetType().Name;
		}
		return "null";
	}

	private static string BuildChecksumCountMismatchMessage(DiscoveredTestCase testCase, NetworkChecksumCapture expected, NetworkChecksumCapture actual)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(37, 1, stringBuilder);
		handler.AppendFormatted(testCase.DisplayName);
		handler.AppendLiteral(" generated different checksum counts.");
		StringBuilder stringBuilder2 = stringBuilder.AppendLine(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(26, 3, stringBuilder2);
		handler2.AppendLiteral("localNetId ");
		handler2.AppendFormatted(expected.LocalNetId);
		handler2.AppendLiteral(": ");
		handler2.AppendFormatted(expected.Checksums.Count);
		handler2.AppendLiteral(" checksums [");
		handler2.AppendFormatted(FormatChecksumContexts(expected.Checksums));
		handler2.AppendLiteral("]");
		StringBuilder stringBuilder3 = stringBuilder2.AppendLine(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(26, 3, stringBuilder3);
		handler3.AppendLiteral("localNetId ");
		handler3.AppendFormatted(actual.LocalNetId);
		handler3.AppendLiteral(": ");
		handler3.AppendFormatted(actual.Checksums.Count);
		handler3.AppendLiteral(" checksums [");
		handler3.AppendFormatted(FormatChecksumContexts(actual.Checksums));
		handler3.AppendLiteral("]");
		return stringBuilder3.AppendLine(ref handler3).ToString();
	}

	private static string BuildChecksumMismatchMessage(DiscoveredTestCase testCase, NetworkChecksumCapture expected, NetworkChecksumCapture actual, int index)
	{
		RecordedChecksum recordedChecksum = expected.Checksums[index];
		RecordedChecksum recordedChecksum2 = actual.Checksums[index];
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(41, 2, stringBuilder);
		handler.AppendFormatted(testCase.DisplayName);
		handler.AppendLiteral(" generated different checksums at index ");
		handler.AppendFormatted(index);
		handler.AppendLiteral(".");
		StringBuilder stringBuilder2 = stringBuilder.AppendLine(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(35, 4, stringBuilder2);
		handler2.AppendLiteral("localNetId ");
		handler2.AppendFormatted(expected.LocalNetId);
		handler2.AppendLiteral(": id=");
		handler2.AppendFormatted(recordedChecksum.Id);
		handler2.AppendLiteral(" checksum=");
		handler2.AppendFormatted(recordedChecksum.Checksum);
		handler2.AppendLiteral(" context=");
		handler2.AppendFormatted(recordedChecksum.Context);
		StringBuilder stringBuilder3 = stringBuilder2.AppendLine(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(35, 4, stringBuilder3);
		handler3.AppendLiteral("localNetId ");
		handler3.AppendFormatted(actual.LocalNetId);
		handler3.AppendLiteral(": id=");
		handler3.AppendFormatted(recordedChecksum2.Id);
		handler3.AppendLiteral(" checksum=");
		handler3.AppendFormatted(recordedChecksum2.Checksum);
		handler3.AppendLiteral(" context=");
		handler3.AppendFormatted(recordedChecksum2.Context);
		return stringBuilder3.AppendLine(ref handler3).ToString();
	}

	private static string FormatChecksumContexts(IReadOnlyList<RecordedChecksum> checksums)
	{
		return string.Join(" | ", checksums.Select((RecordedChecksum checksum) => $"{checksum.Id}:{checksum.Context}"));
	}

	private static string NormalizeChecksumContext(string context)
	{
		return Regex.Replace(context, "\\(\\d+\\)", "(*)");
	}

	private static Exception Unwrap(Exception ex)
	{
		if (ex is TargetInvocationException ex2)
		{
			Exception innerException = ex2.InnerException;
			if (innerException != null)
			{
				return Unwrap(innerException);
			}
		}
		if (ex is AggregateException ex3 && ex3.InnerExceptions.Count == 1)
		{
			Exception ex4 = ex3.InnerExceptions[0];
			if (ex4 != null)
			{
				return Unwrap(ex4);
			}
		}
		return ex;
	}
}
