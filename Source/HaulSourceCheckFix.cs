using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaBugFixes;

public class HaulSourceCheckFix : Fix
{
    private static HaulSourceCheckFix instance;
    private static readonly ConditionalWeakTable<ListerHaulables, StrongBox<int>> cycles = new();

    public override string Id => "HaulSourceCheck";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ListerHaulables), "HaulSourcesCheckTick", new[] { typeof(List<IHaulSource>) })
                ?? throw new System.Exception("ListerHaulables.HaulSourcesCheckTick not found"),
            prefix: new HarmonyMethod(typeof(HaulSourceCheckFix), nameof(Prefix)));
    }

    public static bool Prefix(ListerHaulables __instance, List<IHaulSource> haulList)
    {
        if (!instance.On)
        {
            return true;
        }
        if (haulList.Count == 0)
        {
            return false;
        }
        StrongBox<int> cycle = cycles.GetValue(__instance, _ => new StrongBox<int>());
        cycle.Value = cycle.Value >= int.MaxValue - 1 ? 0 : cycle.Value + 1;
        int groups = Mathf.CeilToInt(haulList.Count / 4f);
        int start = cycle.Value % groups * 4;
        for (int i = 0; i < 4 && start + i < haulList.Count; i++)
        {
            IHaulSource source = haulList[start + i];
            if (source.GetDirectlyHeldThings().Count != 0)
            {
                __instance.RecalculateAllInHaulSource(source);
            }
        }
        return false;
    }
}
