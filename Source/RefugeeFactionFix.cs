using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace VanillaBugFixes;

public class RefugeeFactionFix : Fix
{
    private const string ChanceName = "refugeeFactionChance";
    private const float DefaultChance = 0.6f;

    private static RefugeeFactionFix instance;

    [ThreadStatic]
    private static float? pendingChance;

    private int corrected;

    public override string Id => "RefugeeFaction";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(SitePartWorker_DownedRefugee), nameof(SitePartWorker_DownedRefugee.Notify_GeneratedByQuestGen)),
            prefix: new HarmonyMethod(typeof(RefugeeFactionFix), nameof(NotifyPrefix)),
            finalizer: new HarmonyMethod(typeof(RefugeeFactionFix), nameof(NotifyFinalizer)));
        harmony.Patch(AccessTools.Method(typeof(DownedRefugeeQuestUtility), nameof(DownedRefugeeQuestUtility.GenerateRefugee)),
            prefix: new HarmonyMethod(typeof(RefugeeFactionFix), nameof(GenerateRefugeePrefix)));
    }

    public static void NotifyPrefix(Slate slate, out float? __state)
    {
        __state = pendingChance;
        pendingChance = slate.Exists(ChanceName) ? slate.Get(ChanceName, DefaultChance) : DefaultChance;
    }

    public static Exception NotifyFinalizer(Exception __exception, float? __state)
    {
        pendingChance = __state;
        return __exception;
    }

    public static void GenerateRefugeePrefix(ref float chanceForFaction)
    {
        if (pendingChance is not float chance || !instance.On)
        {
            return;
        }
        pendingChance = null;
        if (chance != chanceForFaction)
        {
            instance.corrected++;
            chanceForFaction = chance;
        }
    }

    public override string Stats() => "VBF_RefugeeFactionStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
