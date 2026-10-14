using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace TestTheSpire;

public static class CombatTestBootstrap
{
	private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30L);

	private static Assembly? _testAssembly;

	private static CombatTestOptions _options = new CombatTestOptions();

	private static bool _initialized;

	private static bool _started;

	internal static string LogPrefix => _options.LogPrefix;

	internal static ZeroActionTestBehavior ZeroActionBehavior => _options.ZeroActionBehavior;

	public static bool Enabled
	{
		get
		{
			if (!CommandLineHelper.HasArg(_options.PrimaryArg))
			{
				if (!string.IsNullOrWhiteSpace(_options.LegacyArg))
				{
					return CommandLineHelper.HasArg(_options.LegacyArg);
				}
				return false;
			}
			return true;
		}
	}

	public static void Initialize(Assembly testAssembly, CombatTestOptions? options = null)
	{
		_testAssembly = testAssembly ?? throw new ArgumentNullException("testAssembly");
		_options = options ?? new CombatTestOptions();
		if (Enabled && !_initialized)
		{
			_initialized = true;
			InstallLocalAssemblyResolver();
			TestMode.TurnOnInternal();
			if (NGame.Instance != null)
			{
				NGame.Instance.StartOnMainMenu = false;
			}
			StartWhenReadyAsync();
		}
	}

	private static async Task StartWhenReadyAsync()
	{
		_ = 1;
		try
		{
			await WaitFor(delegate
			{
				NGame instance = NGame.Instance;
				return ((instance != null) ? instance.RootSceneContainer : null) != null && IsStartupReady();
			}, "Game startup did not finish in time.");
			await StartAsync(NGame.Instance ?? throw new InvalidOperationException("NGame was not available."));
		}
		catch (Exception value)
		{
			Log.Error($"[{LogPrefix}] Bootstrap FAIL\n{value}", 2);
			ImmediateExit(1);
		}
	}

	private static async Task StartAsync(NGame game)
	{
		if (_started)
		{
			return;
		}
		_started = true;
		try
		{
			if (!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warn("[" + LogPrefix + "] Running without native Godot headless mode. Use --headless for CLI runs.", 2);
			}
			SaveManager.Instance.Progress.EnableFtues = false;
			CombatTestRunner combatTestRunner = new CombatTestRunner(game);
			string value = CommandLineHelper.GetValue("sts2-test-filter");
			bool listOnly = CommandLineHelper.HasArg("sts2-test-list");
			Assembly assembly = _testAssembly ?? throw new InvalidOperationException("Combat test assembly was not configured.");
			CombatTestRunSummary combatTestRunSummary = await combatTestRunner.RunAsync(assembly, value, listOnly);
			Log.Info($"[{LogPrefix}] SUMMARY total={combatTestRunSummary.Total} passed={combatTestRunSummary.Passed} failed={combatTestRunSummary.Failed} skipped={combatTestRunSummary.Skipped}", 2);
			ImmediateExit((combatTestRunSummary.Failed != 0) ? 1 : 0);
		}
		catch (Exception value2)
		{
			Log.Error($"[{LogPrefix}] FAIL\n{value2}", 2);
			ImmediateExit(1);
		}
	}

	internal static void TerminateWithError(string message)
	{
		Log.Error("[" + LogPrefix + "] " + message, 2);
		ImmediateExit(1);
	}

	internal static void ImmediateExit(int status)
	{
		Console.Out.Flush();
		Console.Error.Flush();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			if (!TerminateProcess(GetCurrentProcess(), (uint)status))
			{
				throw new InvalidOperationException("TerminateProcess failed.");
			}
		}
		else
		{
			PosixExit(status);
		}
		throw new InvalidOperationException("Immediate process exit returned unexpectedly.");
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GetCurrentProcess();

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool TerminateProcess(nint hProcess, uint uExitCode);

	[DllImport("libc", EntryPoint = "_exit")]
	private static extern void PosixExit(int status);

	private static void InstallLocalAssemblyResolver()
	{
		AppDomain.CurrentDomain.AssemblyResolve -= ResolveFromModDirectory;
		AppDomain.CurrentDomain.AssemblyResolve += ResolveFromModDirectory;
	}

	private static Assembly? ResolveFromModDirectory(object? _, ResolveEventArgs args)
	{
		string location = Assembly.GetExecutingAssembly().Location;
		if (string.IsNullOrWhiteSpace(location))
		{
			return null;
		}
		string directoryName = Path.GetDirectoryName(location);
		if (string.IsNullOrWhiteSpace(directoryName))
		{
			return null;
		}
		string text = new AssemblyName(args.Name).Name ?? string.Empty;
		string text2 = Path.Combine(directoryName, text + ".dll");
		if (!File.Exists(text2))
		{
			return null;
		}
		return AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())?.LoadFromAssemblyPath(text2);
	}

	private static bool IsStartupReady()
	{
		if (SaveManager.Instance.Progress == null || SaveManager.Instance.PrefsSave == null)
		{
			return false;
		}
		try
		{
			_ = SaveManager.Instance.CurrentProfileId;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private static async Task WaitFor(Func<bool> condition, string timeoutMessage)
	{
		DateTime deadline = DateTime.UtcNow + WaitTimeout;
		while (!condition())
		{
			if (DateTime.UtcNow >= deadline)
			{
				throw new InvalidOperationException(timeoutMessage);
			}
			SceneTree val = (SceneTree)Engine.GetMainLoop();
			await ((GodotObject)val).ToSignal((GodotObject)val, "process_frame");
		}
	}
}
