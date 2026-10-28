using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class MeleeDamageTotalFix : Fix
{
    private static MeleeDamageTotalFix instance;
    private static Func<Verb_MeleeAttackDamage, LocalTargetInfo, IEnumerable<DamageInfo>> damageInfos;

    private int corrected;

    public override string Id => "MeleeDamageTotal";

    public override string Category => "VBF_CategoryCombat";

    protected override void Apply(Harmony harmony)
    {
        damageInfos = AccessTools.MethodDelegate<Func<Verb_MeleeAttackDamage, LocalTargetInfo, IEnumerable<DamageInfo>>>(
            AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"));
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget"),
            prefix: new HarmonyMethod(typeof(MeleeDamageTotalFix), nameof(ApplyMeleeDamageToTargetPrefix)));
    }

    public static bool ApplyMeleeDamageToTargetPrefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, ref DamageWorker.DamageResult __result)
    {
        if (!instance.On)
        {
            return true;
        }
        DamageWorker.DamageResult total = new DamageWorker.DamageResult();
        int parts = 0;
        foreach (DamageInfo item in damageInfos(__instance, target))
        {
            if (target.ThingDestroyed)
            {
                break;
            }
            DamageWorker.DamageResult next = target.Thing.TakeDamage(item);
            if (parts > 0)
            {
                Merge(total, next);
            }
            total = next;
            parts++;
        }
        if (parts > 1)
        {
            instance.corrected++;
        }
        __result = total;
        return false;
    }

    private static void Merge(DamageWorker.DamageResult earlier, DamageWorker.DamageResult last)
    {
        last.totalDamageDealt += earlier.totalDamageDealt;
        last.wounded |= earlier.wounded;
        last.headshot |= earlier.headshot;
        if (!earlier.parts.NullOrEmpty())
        {
            foreach (BodyPartRecord part in earlier.parts)
            {
                if (last.parts == null || !last.parts.Contains(part))
                {
                    last.AddPart(earlier.hitThing, part);
                }
            }
        }
        if (!earlier.hediffs.NullOrEmpty())
        {
            foreach (Hediff hediff in earlier.hediffs)
            {
                if (last.hediffs == null || !last.hediffs.Contains(hediff))
                {
                    last.AddHediff(hediff);
                }
            }
        }
    }

    public override string Stats() => "VBF_MeleeDamageTotalStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
