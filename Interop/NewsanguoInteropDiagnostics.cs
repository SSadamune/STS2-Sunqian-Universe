using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// 临时诊断：把新三国联动各环节写进 RitsuLib 日志，定位 Interop 是否绑上。
/// </summary>
internal static class NewsanguoInteropDiagnostics
{
	private const string Prefix = "[NewsanguoInterop] ";
	private const int MaxRepeatingLogs = 6;

	private static int _hoverLogs;
	private static int _noteLogs;

	public static void LogSnapshot(string phase)
	{
		try
		{
			ModTypeDiscoveryHub.LogDiagnostics();
			Info($"{phase}: {BuildSnapshot()}");
		}
		catch (Exception ex)
		{
			Info($"{phase}: snapshot failed: {ex}");
		}
	}

	public static void LogLibraryHover(bool inCombat, bool willShow)
	{
		if (_hoverLogs++ >= MaxRepeatingLogs)
		{
			return;
		}

		Info($"LibraryHoverTips #{_hoverLogs}: IsReady={SafeIsReady()} inCombat={inCombat} willShow={willShow}");
	}

	public static void LogCombatNote(Player? owner, bool appended)
	{
		if (_noteLogs++ >= MaxRepeatingLogs)
		{
			return;
		}

		Info($"CombatNote #{_noteLogs}: IsReady={SafeIsReady()} appended={appended} {DescribePlayer(owner)} {DescribeCharacter(owner?.Character)}");
	}

	public static void LogApply(Creature target, bool swap)
	{
		try
		{
			Player? player = target.Player;
			Info(
				"ApplyVigorOrDrunkenMight: " +
				$"swap={swap} IsReady={SafeIsReady()} " +
				$"target={target.GetType().Name} " +
				$"{DescribePlayer(player)} {DescribeCharacter(player?.Character)}");
		}
		catch (Exception ex)
		{
			Info($"ApplyVigorOrDrunkenMight log failed: {ex}");
		}
	}

	private static string BuildSnapshot()
	{
		var builder = new StringBuilder();
		builder.Append("IsReady=").Append(SafeIsReady());
		builder.Append(" IsReadyHarmony=").Append(DescribeHarmony(
			AccessTools.PropertyGetter(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.IsReady))));
		builder.Append(" IsNewsanguoCharacterHarmony=").Append(DescribeHarmony(
			AccessTools.Method(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.IsNewsanguoCharacter))));
		builder.Append(" ApplyDrunkenMightHarmony=").Append(DescribeHarmony(
			AccessTools.Method(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.ApplyDrunkenMight))));

		string related = string.Join(
			", ",
			AppDomain.CurrentDomain.GetAssemblies()
				.Select(assembly => assembly.GetName().Name ?? "?")
				.Where(name => name.Contains("newsanguo", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("sunqian", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("RitsuLib", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
		builder.Append(" relatedAssemblies=[").Append(related).Append(']');

		Type? apiType = FindType("newsanguo.Scripts.Api.NewsanguoPublicApi");
		builder.Append(" NewsanguoPublicApi=");
		if (apiType == null)
		{
			builder.Append("NOT_FOUND");
			return builder.ToString();
		}

		builder.Append(apiType.AssemblyQualifiedName);
		try
		{
			PropertyInfo? isReady = apiType.GetProperty("IsReady", BindingFlags.Public | BindingFlags.Static);
			builder.Append(" targetIsReady=").Append(isReady?.GetValue(null));
		}
		catch (Exception ex)
		{
			builder.Append(" targetIsReady_error=").Append(ex.GetType().Name);
		}

		return builder.ToString();
	}

	private static Type? FindType(string fullName)
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				Type? type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}

		return null;
	}

	private static bool SafeIsReady()
	{
		try
		{
			return NewsanguoPublicApiInterop.IsReady;
		}
		catch (Exception ex)
		{
			Info($"IsReady threw: {ex}");
			return false;
		}
	}

	private static string DescribeHarmony(MethodBase? method)
	{
		if (method == null)
		{
			return "missing";
		}

		HarmonyLib.Patches? patches = Harmony.GetPatchInfo(method);
		if (patches == null)
		{
			return "none";
		}

		return $"pre={patches.Prefixes.Count}/post={patches.Postfixes.Count}/trans={patches.Transpilers.Count}";
	}

	private static string DescribePlayer(Player? player)
	{
		return player == null ? "player=null" : $"player={player.GetType().Name}";
	}

	private static string DescribeCharacter(CharacterModel? character)
	{
		if (character == null)
		{
			return "character=null";
		}

		string entry = "?";
		try
		{
			entry = character.Id.Entry;
		}
		catch
		{
		}

		try
		{
			bool isNewsanguo = NewsanguoPublicApiInterop.IsNewsanguoCharacter(character);
			return $"character={character.GetType().FullName} entry={entry} IsNewsanguoCharacter={isNewsanguo}";
		}
		catch (Exception ex)
		{
			return $"character={character.GetType().FullName} entry={entry} IsNewsanguoCharacter_threw={ex.GetType().Name}:{ex.Message}";
		}
	}

	private static void Info(string message)
	{
		string line = Prefix + message;
		if (SquMod.Logger != null)
		{
			SquMod.Logger.Info(line);
			return;
		}

		RitsuLibFramework.Logger.Info(line, 1);
	}
}
