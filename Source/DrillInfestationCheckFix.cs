using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class DrillInfestationCheckFix : Fix
{
    private const int LongestWorkInterval = 15;

    private static DrillInfestationCheckFix instance;
    private static AccessTools.FieldRef<CompDeepDrill, int> lastUsedTick;

    private int corrected;

    public override string Id => "DrillInfestationCheck";

    public override string Category => "VBF_CategoryColony";

    public override bool DefaultOn => false;

    protected override void Apply(Harmony harmony)
    {
        lastUsedTick = AccessTools.FieldRefAccess<CompDeepDrill, int>("lastUsedTick");
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(CompDeepDrill), nameof(CompDeepDrill.UsedLastTick)),
            postfix: new HarmonyMethod(typeof(DrillInfestationCheckFix), nameof(UsedLastTickPostfix)));
    }

    public static void UsedLastTickPostfix(CompDeepDrill __instance, ref bool __result)
    {
        if (__result || !instance.On || lastUsedTick(__instance) < Find.TickManager.TicksGame - LongestWorkInterval)
        {
            return;
        }
        instance.corrected++;
        __result = true;
    }

    public override string Stats() => "VBF_DrillInfestationCheckStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
