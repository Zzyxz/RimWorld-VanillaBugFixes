using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class HistoryGoodwillTrimFix : Fix
{
    private static HistoryGoodwillTrimFix instance;
    private static FieldInfo colonistEvents;
    private static FieldInfo factionEvents;
    private static FieldInfo ticksField;
    private static FieldInfo goodwillField;
    private static PropertyInfo indexer;

    public override string Id => "HistoryGoodwillTrim";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        Type records = AccessTools.Inner(typeof(HistoryEventsManager), "HistoryEventRecords")
            ?? throw new Exception("HistoryEventRecords not found");
        ticksField = AccessTools.Field(records, "ticksGame") ?? throw new Exception("ticksGame not found");
        goodwillField = AccessTools.Field(records, "customGoodwill") ?? throw new Exception("customGoodwill not found");
        colonistEvents = AccessTools.Field(typeof(HistoryEventsManager), "colonistEvents") ?? throw new Exception("colonistEvents not found");
        factionEvents = AccessTools.Field(typeof(HistoryEventsManager), "eventsAffectingFaction") ?? throw new Exception("eventsAffectingFaction not found");
        indexer = colonistEvents.FieldType.GetProperty("Item", new[] { typeof(HistoryEventDef) }) ?? throw new Exception("event map indexer not found");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(HistoryEventsManager), nameof(HistoryEventsManager.RecordEvent))
                ?? throw new Exception("HistoryEventsManager.RecordEvent not found"),
            postfix: new HarmonyMethod(typeof(HistoryGoodwillTrimFix), nameof(RecordEventPostfix)));
    }

    public static void RecordEventPostfix(HistoryEventsManager __instance, HistoryEvent historyEvent)
    {
        if (!instance.On)
        {
            return;
        }
        if (historyEvent.args.TryGetArg(HistoryEventArgsNames.Doer, out Pawn doer) && doer.IsColonist)
        {
            instance.Trim(colonistEvents.GetValue(__instance), historyEvent.def);
        }
        if (historyEvent.args.TryGetArg(HistoryEventArgsNames.AffectedFaction, out Faction faction)
            && factionEvents.GetValue(__instance) is IDictionary byFaction && byFaction.Contains(faction))
        {
            instance.Trim(byFaction[faction], historyEvent.def);
        }
    }

    private void Trim(object map, HistoryEventDef def)
    {
        object record = indexer.GetValue(map, new object[] { def });
        if (!(ticksField.GetValue(record) is List<int> ticks) || !(goodwillField.GetValue(record) is List<int> goodwill))
        {
            return;
        }
        if (goodwill.Count > ticks.Count)
        {
            goodwill.RemoveRange(0, goodwill.Count - ticks.Count);
        }
    }
}
