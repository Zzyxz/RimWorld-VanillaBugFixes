using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class PlaceRadiusFix : Fix
{
    private static PlaceRadiusFix instance;

    private int corrected;

    public override string Id => "PlaceRadius";

    public override string Category => "VBF_CategoryMapGeneration";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(GenPlace), nameof(GenPlace.TryPlaceThing), new[]
            {
                typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Action<Thing, int>),
                typeof(Predicate<IntVec3>), typeof(Rot4?), typeof(int),
            }),
            prefix: new HarmonyMethod(typeof(PlaceRadiusFix), nameof(TryPlaceThingPrefix)));
    }

    public static bool TryPlaceThingPrefix(Thing thing, IntVec3 center, Map map, ThingPlaceMode mode, Action<Thing, int> placedAction,
        Predicate<IntVec3> extraValidator, Rot4? rot, int squareRadius, ref bool __result)
    {
        if (squareRadius == 1 || mode != ThingPlaceMode.Radius || !instance.On)
        {
            return true;
        }
        instance.corrected++;
        __result = GenPlace.TryPlaceThing(thing, center, map, mode, out _, placedAction, extraValidator, rot, squareRadius);
        return false;
    }

    public override string Stats() => "VBF_PlaceRadiusStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
