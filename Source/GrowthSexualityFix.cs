using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class GrowthSexualityFix : Fix
{
    private static GrowthSexualityFix instance;

    private int corrected;

    public override string Id => "GrowthSexuality";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "BirthdayBiological")
                ?? throw new Exception("Pawn_AgeTracker.BirthdayBiological not found"),
            prefix: new HarmonyMethod(typeof(GrowthSexualityFix), nameof(Prefix)),
            postfix: new HarmonyMethod(typeof(GrowthSexualityFix), nameof(Postfix)));
    }

    public static void Prefix(Pawn ___pawn, int birthdayAge, out bool __state)
    {
        __state = instance.On
            && ModsConfig.BiotechActive
            && birthdayAge == 13
            && GrowthUtility.IsGrowthBirthday(birthdayAge)
            && ___pawn?.story != null
            && ___pawn.RaceProps.Humanlike
            && (!PawnUtility.ShouldSendNotificationAbout(___pawn) || ___pawn.Faction != Faction.OfPlayer || ___pawn.IsQuestLodger());
    }

    public static void Postfix(Pawn ___pawn, bool __state)
    {
        if (!__state || ___pawn.Dead)
        {
            return;
        }
        PawnGenerator.TryGenerateSexualityTraitFor(___pawn, allowGay: true);
        instance.corrected++;
    }

    public override string Stats() => "VBF_GrowthSexualityStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
