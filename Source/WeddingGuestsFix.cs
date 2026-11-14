using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class WeddingGuestsFix : Fix
{
    private static WeddingGuestsFix instance;
    private static AccessTools.FieldRef<LordJob_Joinable_MarriageCeremony, int> secondCount;

    public override string Id => "WeddingGuests";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        secondCount = AccessTools.FieldRefAccess<LordJob_Joinable_MarriageCeremony, int>("secondPawnSpouseCount");
        var ctor = AccessTools.Constructor(typeof(LordJob_Joinable_MarriageCeremony), new[] { typeof(Pawn), typeof(Pawn), typeof(IntVec3) })
            ?? throw new System.Exception("LordJob_Joinable_MarriageCeremony constructor not found");
        harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(WeddingGuestsFix), nameof(ConstructorPostfix)));
    }

    public static void ConstructorPostfix(LordJob_Joinable_MarriageCeremony __instance, Pawn secondPawn)
    {
        if (!instance.On || secondPawn == null)
        {
            return;
        }
        int count = secondPawn.GetSpouseCount(includeDead: false);
        if (count != secondCount(__instance))
        {
            secondCount(__instance) = count;
        }
    }
}
