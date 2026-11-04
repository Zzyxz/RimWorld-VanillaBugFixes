using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class ChildShamblersSaveFix : Fix
{
    private static ChildShamblersSaveFix instance;
    private static AccessTools.FieldRef<Difficulty, bool> allowed;

    private int corrected;

    public override string Id => "ChildShamblersSave";

    public override string Category => "VBF_CategorySaves";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        allowed = AccessTools.FieldRefAccess<Difficulty, bool>("childShamblersAllowed");
        var expose = AccessTools.DeclaredMethod(typeof(Difficulty), nameof(Difficulty.ExposeData))
            ?? throw new System.Exception("Difficulty.ExposeData not found");
        harmony.Patch(expose, postfix: new HarmonyMethod(typeof(ChildShamblersSaveFix), nameof(ExposeDataPostfix)));
    }

    public static void ExposeDataPostfix(Difficulty __instance)
    {
        if (!instance.On)
        {
            return;
        }
        Scribe_Values.Look(ref allowed(__instance), "childShamblersAllowed", true);
        if (Scribe.mode == LoadSaveMode.LoadingVars && !allowed(__instance))
        {
            instance.corrected++;
        }
    }

    public override string Stats() => "VBF_ChildShamblersSaveStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
