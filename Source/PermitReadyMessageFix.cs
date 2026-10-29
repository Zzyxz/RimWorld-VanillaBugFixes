using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class PermitReadyMessageFix : Fix
{
    private static PermitReadyMessageFix instance;

    private int sent;

    public override string Id => "PermitReadyMessage";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(Pawn_RoyaltyTracker), "RoyaltyTrackerTickInterval", new[] { typeof(int) }),
            postfix: new HarmonyMethod(typeof(PermitReadyMessageFix), nameof(TickIntervalPostfix)));
    }

    public static void TickIntervalPostfix(Pawn_RoyaltyTracker __instance, int delta)
    {
        if (delta <= 1 || !instance.On)
        {
            return;
        }
        Pawn pawn = __instance.pawn;
        if (pawn == null || !pawn.Spawned || pawn.RaceProps.Animal)
        {
            return;
        }
        int now = Find.TickManager.TicksGame;
        foreach (FactionPermit permit in __instance.AllFactionPermits)
        {
            if (permit.LastUsedTick <= 0)
            {
                continue;
            }
            int ready = permit.LastUsedTick + permit.Permit.CooldownTicks;
            if (ready > now - delta && ready < now)
            {
                instance.sent++;
                Messages.Message("MessagePermitCooldownFinished".Translate(pawn, permit.Permit.LabelCap), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    public override string Stats() => "VBF_PermitReadyMessageStats".Translate(sent);

    public override void ResetStats()
    {
        sent = 0;
    }
}
