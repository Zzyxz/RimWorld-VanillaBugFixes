using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class RoyalAscentRefireFix : Fix
{
    private static RoyalAscentRefireFix instance;
    private static AccessTools.FieldRef<StorytellerComp_RefiringUniqueQuest, bool> generateSkipped;

    private int corrected;

    public override string Id => "RoyalAscentRefire";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        generateSkipped = AccessTools.FieldRefAccess<StorytellerComp_RefiringUniqueQuest, bool>("generateSkipped");
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(StorytellerComp_RefiringUniqueQuest), nameof(StorytellerComp_RefiringUniqueQuest.MakeIntervalIncidents)),
            prefix: new HarmonyMethod(typeof(RoyalAscentRefireFix), nameof(MakeIntervalIncidentsPrefix)));
    }

    public static bool MakeIntervalIncidentsPrefix(StorytellerComp_RefiringUniqueQuest __instance, IIncidentTarget target, ref IEnumerable<FiringIncident> __result)
    {
        if (!instance.On)
        {
            return true;
        }
        __result = MakeIntervalIncidents(__instance, target);
        return false;
    }

    private static IEnumerable<FiringIncident> MakeIntervalIncidents(StorytellerComp_RefiringUniqueQuest comp, IIncidentTarget target)
    {
        var props = (StorytellerCompProperties_RefiringUniqueQuest)comp.props;
        if (!props.incident.TargetAllowed(target))
        {
            yield break;
        }
        Quest newest = null;
        Quest first = null;
        List<Quest> quests = Find.QuestManager.QuestsListForReading;
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].root != props.incident.questScriptDef)
            {
                continue;
            }
            first ??= quests[i];
            if (newest == null || quests[i].appearanceTick > newest.appearanceTick)
            {
                newest = quests[i];
            }
        }
        int intervalsPassed = Find.TickManager.TicksGame / 1000;
        bool fire;
        if (newest == null)
        {
            fire = (props.minColonyWealth <= 0 || !(WealthUtility.PlayerWealth < props.minColonyWealth))
                && (!generateSkipped(comp) ? intervalsPassed == (int)(props.minDaysPassed * 60f) + 1 : GenTicks.TicksGame >= props.minDaysPassed * 60000f);
        }
        else
        {
            fire = props.refireEveryDays >= 0f && newest.State != QuestState.EndedSuccess && newest.State != QuestState.Ongoing
                && newest.State != QuestState.NotYetAccepted && newest.cleanupTick >= 0
                && intervalsPassed == (int)(newest.cleanupTick + props.refireEveryDays * 60000f) / 1000;
        }
        if (!fire)
        {
            yield break;
        }
        IncidentParms parms = comp.GenerateParms(props.incident.category, target);
        if (props.incident.Worker.CanFireNow(parms))
        {
            if (newest != first)
            {
                instance.corrected++;
            }
            yield return new FiringIncident(props.incident, comp, parms);
        }
    }

    public override string Stats() => "VBF_RoyalAscentRefireStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
