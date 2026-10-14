using System;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method)]
public class FactAttribute : Attribute
{
	public string? DisplayName { get; init; }

	public string? Skip { get; init; }
}
