using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class SuppressionXpFix : Fix
{
    private static SuppressionXpFix instance;

    public override string Id => "SuppressionXp";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(JobDriver_ActivitySuppression), "TrySuppress")
                ?? throw new Exception("JobDriver_ActivitySuppression.TrySuppress not found"),
            postfix: new HarmonyMethod(typeof(SuppressionXpFix), nameof(TrySuppressPostfix)));
    }

    public static void TrySuppressPostfix(ref Toil __result)
    {
        Toil toil = __result;
        Action<int> original = toil.tickIntervalAction;
        if (original == null)
        {
            return;
        }
        toil.tickIntervalAction = delta =>
        {
            original(delta);
            if (delta <= 1 || !instance.On)
            {
                return;
            }
            Pawn actor = toil.actor;
            if (actor?.skills == null || actor.CurJob == null)
            {
                return;
            }
            actor.skills.Learn(SkillDefOf.Intellectual, 0.1f * (delta - 1));
        };
    }
}
