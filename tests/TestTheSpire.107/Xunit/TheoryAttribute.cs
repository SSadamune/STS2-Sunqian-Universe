using System;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TheoryAttribute : FactAttribute
{
}
