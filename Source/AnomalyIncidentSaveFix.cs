using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class AnomalyIncidentSaveFix : Fix
{
    private static AnomalyIncidentSaveFix instance;
    private static AccessTools.FieldRef<GameComponent_Anomaly, int> incidentTick;

    public override string Id => "AnomalyIncidentSave";

    public override string Category => "VBF_CategorySaves";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        incidentTick = AccessTools.FieldRefAccess<GameComponent_Anomaly, int>("fireLevelIncidentTick");
        var expose = AccessTools.DeclaredMethod(typeof(GameComponent_Anomaly), nameof(GameComponent_Anomaly.ExposeData))
            ?? throw new System.Exception("GameComponent_Anomaly.ExposeData not found");
        harmony.Patch(expose, postfix: new HarmonyMethod(typeof(AnomalyIncidentSaveFix), nameof(ExposeDataPostfix)));
    }

    public static void ExposeDataPostfix(GameComponent_Anomaly __instance)
    {
        if (!instance.On)
        {
            return;
        }
        Scribe_Values.Look(ref incidentTick(__instance), "fireLevelIncidentTick", -99999);
    }
}
