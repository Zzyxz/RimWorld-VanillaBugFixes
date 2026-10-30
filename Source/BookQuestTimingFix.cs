using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class BookQuestTimingFix : Fix
{
    private static BookQuestTimingFix instance;
    private static int readDelta = 1;

    private int corrected;

    public override string Id => "BookQuestTiming";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        var readTick = AccessTools.DeclaredMethod(typeof(Book), nameof(Book.OnBookReadTick))
            ?? throw new Exception("Book.OnBookReadTick not found");
        var questTick = AccessTools.DeclaredMethod(typeof(BookOutcomeDoer_GiveQuest), nameof(BookOutcomeDoer_GiveQuest.OnReadingTick))
            ?? throw new Exception("BookOutcomeDoer_GiveQuest.OnReadingTick not found");
        instance = this;
        harmony.Patch(questTick, transpiler: new HarmonyMethod(typeof(BookQuestTimingFix), nameof(Transpiler)));
        harmony.Patch(readTick,
            prefix: new HarmonyMethod(typeof(BookQuestTimingFix), nameof(ReadTickPrefix)),
            postfix: new HarmonyMethod(typeof(BookQuestTimingFix), nameof(ReadTickPostfix)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo mtb = AccessTools.Method(typeof(Rand), nameof(Rand.MTBEventOccurs), new[] { typeof(float), typeof(float), typeof(float) });
        MethodInfo duration = AccessTools.Method(typeof(BookQuestTimingFix), nameof(CheckDuration));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => i >= 3 && codes[i].Calls(mtb)
            && codes[i - 1].opcode == OpCodes.Ldc_R4 && (float)codes[i - 1].operand == 1f
            && codes[i - 2].opcode == OpCodes.Ldc_R4 && (float)codes[i - 2].operand == 1f
            && codes[i - 3].opcode == OpCodes.Ldc_R4, "quest chance check");
        codes[at - 1] = new CodeInstruction(OpCodes.Call, duration).MoveLabelsFrom(codes[at - 1]);
        return codes;
    }

    public static void ReadTickPrefix(int delta)
    {
        readDelta = delta;
    }

    public static void ReadTickPostfix()
    {
        readDelta = 1;
    }

    public static float CheckDuration()
    {
        if (readDelta <= 1 || !instance.On)
        {
            return 1f;
        }
        instance.corrected++;
        return readDelta;
    }

    public override string Stats() => "VBF_BookQuestTimingStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
