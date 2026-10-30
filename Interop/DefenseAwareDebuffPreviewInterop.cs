using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// Optional UI-only bridge to DefenseAwareDebuffPreview v0.1.1's public API.
/// Reflection keeps the other mod optional and prevents its assembly from being bundled here.
/// </summary>
internal static class DefenseAwareDebuffPreviewInterop
{
	private const string AssemblyName = "DefenseAwareDebuffPreview";
	private const string SourceTypeName =
		"DefenseAwareDebuffPreview.Api.DamagePreviewSource`1";
	private const string ResultTypeName =
		"DefenseAwareDebuffPreview.Api.DamagePreviewResult`1";
	private const string ApiTypeName =
		"DefenseAwareDebuffPreview.Api.DamageDebuffPreviewApi";

	private enum PreviewBlockTiming
	{
		Current,
		AfterAutomaticClear,
	}

	private sealed record ApiBinding(
		Type SourceType,
		ConstructorInfo SourceConstructor,
		MethodInfo TryCalculateBySource,
		MethodInfo GetHpLoss,
		object CurrentBlockTiming,
		object AfterAutomaticClearBlockTiming);

	private static readonly object BindingLock = new();
	private static Assembly? _resolvedAssembly;
	private static ApiBinding? _binding;
	private static bool _resolutionFailed;
	private static bool _invocationFailureLogged;
	private static bool _assemblyLookupNeeded = true;

	static DefenseAwareDebuffPreviewInterop()
	{
		// Also cache absence: health-bar refreshes must not enumerate all loaded
		// assemblies every time. A late optional-mod load invalidates that result.
		AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
		{
			if (string.Equals(args.LoadedAssembly.GetName().Name, AssemblyName,
				StringComparison.OrdinalIgnoreCase))
			{
				lock (BindingLock)
					_assemblyLookupNeeded = true;
			}
		};
	}

	/// <summary>
	/// Calculates UI-only HP loss for the requested debuff. Enemy Poison and Burning
	/// share one ordered turn-start batch. Player and pet Poison run at turn start,
	/// while their Burning runs at player turn end, so those batches stay separate.
	/// </summary>
	public static bool TryGetPreviewHpLoss(
		Creature target,
		PowerModel source,
		out int hpLoss)
	{
		hpLoss = 0;
		if (!TryGetBinding(out ApiBinding binding)
			|| !TryCollectBatch(target, source, out var sources, out PreviewBlockTiming timing))
		{
			return false;
		}

		try
		{
			Array apiSources = CreateApiSources(binding, sources);
			object blockTiming = timing == PreviewBlockTiming.AfterAutomaticClear
				? binding.AfterAutomaticClearBlockTiming
				: binding.CurrentBlockTiming;
			object?[] arguments =
			[
				target,
				apiSources,
				null,
				blockTiming,
				ReferenceEqualityComparer.Instance,
			];
			bool succeeded = (bool)(binding.TryCalculateBySource.Invoke(
				null,
				arguments) ?? false);
			object? result = arguments[2];
			if (!succeeded || result is null)
			{
				return false;
			}

			hpLoss = (int)(binding.GetHpLoss.Invoke(result, [source]) ?? 0);
			return true;
		}
		catch (Exception exception)
		{
			if (!_invocationFailureLogged)
			{
				_invocationFailureLogged = true;
				SquMod.Logger?.Error(
					$"Failed to call DefenseAwareDebuffPreview's public UI API: {exception}");
			}

			return false;
		}
	}

	private static bool TryCollectBatch(
		Creature target,
		PowerModel requestedSource,
		out List<(PowerModel Source, IEnumerable<decimal> Hits, ValueProp Props)> sources,
		out PreviewBlockTiming blockTiming)
	{
		sources = [];
		bool sharedEnemyTurnStart = target.IsEnemy && !target.IsPet;
		if (sharedEnemyTurnStart)
		{
			blockTiming = PreviewBlockTiming.AfterAutomaticClear;
			foreach (PowerModel power in target.Powers)
			{
				TryAddSource(target, power, sources);
			}
		}
		else if (requestedSource is PoisonPower)
		{
			blockTiming = PreviewBlockTiming.AfterAutomaticClear;
			TryAddSource(target, requestedSource, sources);
		}
		else if (requestedSource is BurningPower)
		{
			blockTiming = PreviewBlockTiming.Current;
			TryAddSource(target, requestedSource, sources);
		}
		else
		{
			blockTiming = default;
			return false;
		}

		return sources.Any(item => ReferenceEquals(item.Source, requestedSource));
	}

	private static void TryAddSource(
		Creature target,
		PowerModel power,
		List<(PowerModel Source, IEnumerable<decimal> Hits, ValueProp Props)> sources)
	{
		switch (power)
		{
			case PoisonPower poison when poison.Amount > 0:
				int triggerCount = GetPoisonTriggerCount(target, poison);
				sources.Add((
					poison,
					Enumerable.Range(0, triggerCount)
						.Select(index => (decimal)(poison.Amount - index)),
					ValueProp.Unblockable | ValueProp.Unpowered));
				break;

			case BurningPower burning when burning.Amount > 0:
				sources.Add((burning, [burning.Amount], ValueProp.Unpowered));
				break;
		}
	}

	private static int GetPoisonTriggerCount(Creature target, PoisonPower poison)
	{
		int extraTriggers = target.CombatState?.GetOpponentsOf(target)
			.Where(static creature => creature.IsAlive)
			.Sum(static creature => creature.GetPowerAmount<AccelerantPower>()) ?? 0;

		return Math.Min(poison.Amount, 1 + extraTriggers);
	}

	private static Array CreateApiSources(
		ApiBinding binding,
		List<(PowerModel Source, IEnumerable<decimal> Hits, ValueProp Props)> sources)
	{
		Array apiSources = Array.CreateInstance(binding.SourceType, sources.Count);
		for (int index = 0; index < sources.Count; index++)
		{
			var source = sources[index];
			object apiSource = binding.SourceConstructor.Invoke(
			[
				source.Source,
				source.Hits,
				source.Props,
				null,
				null,
				null,
			]);
			apiSources.SetValue(apiSource, index);
		}

		return apiSources;
	}

	private static bool TryGetBinding(out ApiBinding binding)
	{
		lock (BindingLock)
		{
			if (!_assemblyLookupNeeded)
			{
				binding = _binding!;
				return _binding is not null && !_resolutionFailed;
			}
			_assemblyLookupNeeded = false;
			Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(static candidate => string.Equals(
					candidate.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase));
			if (assembly is null)
			{
				binding = null!;
				return false;
			}

			if (ReferenceEquals(assembly, _resolvedAssembly))
			{
				binding = _binding!;
				return _binding is not null && !_resolutionFailed;
			}

			_resolvedAssembly = assembly;
			_binding = null;
			_resolutionFailed = false;

			try
			{
				_binding = ResolveBinding(assembly);
			}
			catch (Exception exception)
			{
				_resolutionFailed = true;
				SquMod.Logger?.Error(
					$"DefenseAwareDebuffPreview is loaded, but its public API could not be resolved: {exception}");
			}

			binding = _binding!;
			return _binding is not null && !_resolutionFailed;
		}
	}

	private static ApiBinding ResolveBinding(Assembly assembly)
	{
		Type sourceType = assembly.GetType(SourceTypeName, throwOnError: true)!
			.MakeGenericType(typeof(PowerModel));
		Type resultType = assembly.GetType(ResultTypeName, throwOnError: true)!
			.MakeGenericType(typeof(PowerModel));
		Type apiType = assembly.GetType(ApiTypeName, throwOnError: true)!;

		ConstructorInfo sourceConstructor = sourceType.GetConstructor(
		[
			typeof(PowerModel),
			typeof(IEnumerable<decimal>),
			typeof(ValueProp),
			typeof(Creature),
			typeof(CardModel),
			typeof(CardPlay),
		]) ?? throw new MissingMethodException(sourceType.FullName, ".ctor");

		MethodInfo[] publicApiMethods =
			apiType.GetMethods(BindingFlags.Public | BindingFlags.Static);
		MethodInfo tryCalculate = publicApiMethods.Single(method =>
			method.Name == "TryCalculateBySource"
			&& method.IsGenericMethodDefinition
			&& method.GetGenericArguments().Length == 1
			&& method.GetParameters().Length == 5)
			.MakeGenericMethod(typeof(PowerModel));
		Type blockTimingType = tryCalculate.GetParameters()[3].ParameterType;
		object currentBlockTiming = Enum.Parse(blockTimingType, "Current");
		object afterAutomaticClearBlockTiming =
			Enum.Parse(blockTimingType, "AfterAutomaticClear");
		MethodInfo getHpLoss = resultType.GetMethod(
			"GetHpLoss",
			BindingFlags.Public | BindingFlags.Instance,
			binder: null,
			types: [typeof(PowerModel)],
			modifiers: null)
			?? throw new MissingMethodException(resultType.FullName, "GetHpLoss");

		return new ApiBinding(
			sourceType,
			sourceConstructor,
			tryCalculate,
			getHpLoss,
			currentBlockTiming,
			afterAutomaticClearBlockTiming);
	}
}
