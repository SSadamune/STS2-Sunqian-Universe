using System;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InlineDataAttribute : Attribute
{
	public object?[] Data { get; }

	public InlineDataAttribute(params object?[] data)
	{
		Data = data;
	}
}
