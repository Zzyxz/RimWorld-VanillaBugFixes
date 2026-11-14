using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class BookRewardsFix : Fix
{
    private static BookRewardsFix instance;
    private static AccessTools.FieldRef<ThingSetMaker_Books, float> marketValueFactor;
    private static float cheapestBook = -1f;

    public override string Id => "BookRewards";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        marketValueFactor = AccessTools.FieldRefAccess<ThingSetMaker_Books, float>("marketValueFactor");
        var canGenerate = AccessTools.DeclaredMethod(typeof(ThingSetMaker_Books), "CanGenerateSub")
            ?? throw new Exception("ThingSetMaker_Books.CanGenerateSub not found");
        instance = this;
        harmony.Patch(canGenerate, postfix: new HarmonyMethod(typeof(BookRewardsFix), nameof(CanGenerateSubPostfix)));
    }

    private static float CheapestBook
    {
        get
        {
            if (cheapestBook < 0f)
            {
                var books = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.HasComp<CompBook>()).ToList();
                cheapestBook = books.Count > 0 ? books.Min(d => d.BaseMarketValue) : float.MaxValue;
            }
            return cheapestBook;
        }
    }

    private static bool BookFits(ThingSetMaker_Books maker, ThingSetMakerParams parms)
    {
        if (parms.countRange.HasValue && parms.countRange.Value.max <= 0)
        {
            return false;
        }
        return !parms.totalMarketValueRange.HasValue || parms.totalMarketValueRange.Value.max * marketValueFactor(maker) >= CheapestBook;
    }

    public static void CanGenerateSubPostfix(ThingSetMaker_Books __instance, ThingSetMakerParams parms, ref bool __result)
    {
        if (!__result && instance.On && BookFits(__instance, parms))
        {
            __result = true;
        }
    }
}
