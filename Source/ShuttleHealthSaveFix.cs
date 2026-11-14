using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class ShuttleHealthSaveFix : Fix
{
    private static ShuttleHealthSaveFix instance;

    public override string Id => "ShuttleHealthSave";

    public override string Category => "VBF_CategorySaves";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        var expose = AccessTools.DeclaredMethod(typeof(CompShuttle), nameof(CompShuttle.PostExposeData))
            ?? throw new System.Exception("CompShuttle.PostExposeData not found");
        harmony.Patch(expose, postfix: new HarmonyMethod(typeof(ShuttleHealthSaveFix), nameof(ExposeDataPostfix)));
    }

    public static void ExposeDataPostfix(CompShuttle __instance)
    {
        if (!instance.On)
        {
            return;
        }
        Scribe_Values.Look(ref __instance.onlyAcceptHealthy, "onlyAcceptHealthy", false);
    }
}
