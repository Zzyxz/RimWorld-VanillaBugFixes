using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class TerrorRemovalFix : Fix
{
    private static TerrorRemovalFix instance;

    private int corrected;

    public override string Id => "TerrorRemoval";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(TerrorUtility), nameof(TerrorUtility.RemoveAllTerrorThoughts))
                ?? throw new Exception("TerrorUtility.RemoveAllTerrorThoughts not found"),
            prefix: new HarmonyMethod(typeof(TerrorRemovalFix), nameof(Prefix)));
    }

    public static bool Prefix(Pawn pawn)
    {
        if (!instance.On)
        {
            return true;
        }
        MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories == null)
        {
            return false;
        }
        var thoughts = TerrorUtility.GetTerrorThoughts(pawn).ToList();
        foreach (Thought_MemoryObservationTerror thought in thoughts)
        {
            memories.RemoveMemory(thought);
        }
        if (thoughts.Count > 1)
        {
            instance.corrected++;
        }
        return false;
    }

    public override string Stats() => "VBF_TerrorRemovalStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
