using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace TestTheSpire;

[ModInitializer("Init")]
public static class TestTheSpireMod
{
	public static void Init()
	{
		Log.Info("[TestTheSpire] Loaded", 2);
	}
}
