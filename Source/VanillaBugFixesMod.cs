using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace VanillaBugFixes;

public abstract class Fix
{
    public abstract string Id { get; }

    public abstract string Category { get; }

    public bool Active { get; private set; }
    public string Problem { get; private set; }

    public virtual bool DefaultOn => true;

    public bool On => VanillaBugFixesMod.Settings.IsOn(Id, DefaultOn);

    protected abstract void Apply(Harmony harmony);

    public void TryApply(Harmony harmony)
    {
        try
        {
            Apply(harmony);
            Active = true;
        }
        catch (Exception e)
        {
            Problem = e.Message;
            Log.Warning("[Vanilla Bug Fixes] " + Id + " is not active: " + e);
        }
    }

    protected static int SingleMatch(List<CodeInstruction> codes, Func<int, bool> match, string what)
    {
        int found = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            if (!match(i))
            {
                continue;
            }
            if (found >= 0)
            {
                throw new Exception(what + " is ambiguous");
            }
            found = i;
        }
        if (found < 0)
        {
            throw new Exception(what + " not found");
        }
        return found;
    }

    public virtual string Stats() => null;

    public virtual void ResetStats()
    {
    }
}

public class VanillaBugFixesMod : Mod
{
    public static FixSettings Settings;

    public static readonly List<Fix> Fixes = new()
    {
        new StaggerStartFix(),
        new PanicFleeFix(),
        new ShieldDomeReachFix(),
        new MeleeDamageTotalFix(),
        new JobClearFix(),
        new PrioritySorterFix(),
        new TrainFoodOrderFix(),
        new PlaceRadiusFix(),
        new SettlementInhabitantsFix(),
        new RoyalAscentRefireFix(),
        new RefugeeFactionFix(),
        new PermitReadyMessageFix(),
        new BookRewardsFix(),
        new SowNeighborProtectFix(),
        new DrillInfestationCheckFix(),
        new EmpBatteryDischargeFix(),
        new BookQuestTimingFix(),
    };

    private Vector2 scroll;
    private float viewHeight = 1000f;

    public VanillaBugFixesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FixSettings>();
        var harmony = new Harmony("Zzyxzz.VanillaBugFixes");
        foreach (var fix in Fixes)
        {
            fix.TryApply(harmony);
        }
    }

    public override string SettingsCategory() => "Vanilla Bug Fixes";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var view = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
        Widgets.BeginScrollView(inRect, ref scroll, view);
        var list = new Listing_Standard();
        list.Begin(view);
        list.Label("VBF_Intro".Translate());
        list.GapLine();

        foreach (var group in Fixes.GroupBy(f => f.Category))
        {
            Text.Font = GameFont.Medium;
            list.Label(group.Key.Translate());
            Text.Font = GameFont.Small;
            foreach (var fix in group)
            {
                bool on = fix.On;
                bool before = on;
                list.CheckboxLabeled(("VBF_" + fix.Id).Translate(), ref on, ("VBF_" + fix.Id + "Tip").Translate());
                if (on != before)
                {
                    Settings.Set(fix.Id, on);
                }
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                if (fix.Problem != null)
                {
                    list.Label("VBF_NotActive".Translate(fix.Problem));
                }
                else if (!fix.On)
                {
                    list.Label("VBF_SwitchedOff".Translate());
                }
                else
                {
                    list.Label(fix.Stats() ?? "VBF_Active".Translate().ToString());
                }
                GUI.color = Color.white;
                list.Gap(8f);
            }
            list.GapLine();
        }

        if (list.ButtonText("VBF_ResetStats".Translate()))
        {
            foreach (var fix in Fixes)
            {
                fix.ResetStats();
            }
        }
        viewHeight = list.CurHeight + 20f;
        list.End();
        Widgets.EndScrollView();
    }
}

public class FixSettings : ModSettings
{
    private Dictionary<string, bool> enabled = new();

    public bool IsOn(string id, bool defaultOn) => enabled.TryGetValue(id, out bool on) ? on : defaultOn;

    public void Set(string id, bool on)
    {
        enabled[id] = on;
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref enabled, "enabled", LookMode.Value, LookMode.Value);
        enabled ??= new Dictionary<string, bool>();
    }
}
