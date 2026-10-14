using System;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NetworkChecksumFactAttribute : FactAttribute
{
	public int[] LocalNetIds { get; }

	public bool ExpectMismatch { get; set; }

	public bool CompareContextsOnly { get; set; }

	public NetworkChecksumFactAttribute(params int[] localNetIds)
	{
		LocalNetIds = localNetIds;
	}
}
