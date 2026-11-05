using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class ConcertSpotFix : Fix
{
    private static ConcertSpotFix instance;

    private int corrected;

    public override string Id => "ConcertSpot";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(GatheringWorker_Concert), "TryFindGatherSpot")
                ?? throw new System.Exception("GatheringWorker_Concert.TryFindGatherSpot not found"),
            prefix: new HarmonyMethod(typeof(ConcertSpotFix), nameof(Prefix)));
    }

    public static bool Prefix(GatheringWorker_Concert __instance, Pawn organizer, out IntVec3 spot, ref bool __result)
    {
        spot = IntVec3.Invalid;
        if (!instance.On)
        {
            return true;
        }
        bool enjoyableOutside = JoyUtility.EnjoyableOutsideNow(organizer);
        var best = new List<Building_MusicalInstrument>();
        float range = -1f;
        foreach (Building_MusicalInstrument item in organizer.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MusicalInstrument>())
        {
            if (!GatheringsUtility.ValidateGatheringSpot(item.InteractionCell, __instance.def, organizer, enjoyableOutside, ignoreRequiredColonistCount: false)
                || !GatheringWorker_Concert.InstrumentAccessible(item, organizer))
            {
                continue;
            }
            float itemRange = item.def.building.instrumentRange;
            if (itemRange < range)
            {
                continue;
            }
            if (itemRange > range)
            {
                best.Clear();
                range = itemRange;
            }
            best.Add(item);
        }
        if (!best.TryRandomElement(out Building_MusicalInstrument result))
        {
            __result = false;
            return false;
        }
        spot = result.InteractionCell;
        __result = true;
        instance.corrected++;
        return false;
    }

    public override string Stats() => "VBF_ConcertSpotStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
