using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using TestTheSpire;

namespace Squ.Tests;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public static void Init()
    {
        CombatTestBootstrap.Initialize(
            Assembly.GetExecutingAssembly(),
            new CombatTestOptions
            {
                LogPrefix = "sunqian-universe.Tests",
                ZeroActionBehavior = ZeroActionTestBehavior.EnqueueCleanupNoOp
            });

        Log.Info("[sunqian-universe.Tests] Mod initialized");
    }
}
