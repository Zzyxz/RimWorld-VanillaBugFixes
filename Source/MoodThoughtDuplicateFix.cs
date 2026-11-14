using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class MoodThoughtDuplicateFix : Fix
{
    private static MoodThoughtDuplicateFix instance;
    private static AccessTools.FieldRef<SituationalThoughtHandler, List<Thought_Situational>> cachedThoughts;

    public override string Id => "MoodThoughtDuplicate";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        cachedThoughts = AccessTools.FieldRefAccess<SituationalThoughtHandler, List<Thought_Situational>>("cachedThoughts");
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(SituationalThoughtHandler), nameof(SituationalThoughtHandler.AppendMoodThoughts), new[] { typeof(ThoughtDef), typeof(List<Thought>) })
                ?? throw new Exception("SituationalThoughtHandler.AppendMoodThoughts not found"),
            prefix: new HarmonyMethod(typeof(MoodThoughtDuplicateFix), nameof(Prefix)));
    }

    public static void Prefix(SituationalThoughtHandler __instance, ThoughtDef def)
    {
        if (!instance.On)
        {
            return;
        }
        List<Thought_Situational> thoughts = cachedThoughts(__instance);
        for (int i = 0; i < thoughts.Count; i++)
        {
            Thought_Situational thought = thoughts[i];
            if (thought.def == def && !thought.Active)
            {
                thought.RecalculateState();
            }
        }
    }
}
