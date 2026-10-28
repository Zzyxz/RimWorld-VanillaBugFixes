using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class StaggerStartFix : Fix
{
    private static StaggerStartFix instance;
    private static AccessTools.FieldRef<StaggerHandler, int> ticksLeft;
    private static AccessTools.FieldRef<StaggerHandler, float> speedFactor;

    private int corrected;

    public override string Id => "StaggerStart";

    public override string Category => "VBF_CategoryCombat";

    protected override void Apply(Harmony harmony)
    {
        ticksLeft = AccessTools.FieldRefAccess<StaggerHandler, int>("staggerTicksLeft");
        speedFactor = AccessTools.FieldRefAccess<StaggerHandler, float>("staggerMoveSpeedFactor");
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(StaggerHandler), nameof(StaggerHandler.StaggerFor)),
            prefix: new HarmonyMethod(typeof(StaggerStartFix), nameof(StaggerForPrefix)));
    }

    public static void StaggerForPrefix(StaggerHandler __instance, float moveSpeedFactor)
    {
        if (ticksLeft(__instance) > 0 || !instance.On)
        {
            return;
        }
        ref float factor = ref speedFactor(__instance);
        if (factor < moveSpeedFactor)
        {
            instance.corrected++;
        }
        factor = moveSpeedFactor;
    }

    public override string Stats() => "VBF_StaggerStartStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
