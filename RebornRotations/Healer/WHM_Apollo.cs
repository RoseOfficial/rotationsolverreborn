using Dalamud.Interface.Colors;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace RebornRotations.Healer;

[Rotation("Apollo", CombatType.PvE, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/Healer/WHM_Apollo.cs")]
[Api(5)]
public sealed class WHM_Apollo : WhiteMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Presence of Mind for damage boost")]
    public bool UsePresenceOfMind { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use DOT spells (Aero/Dia)")]
    public bool UseDOTs { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Refresh DOT early when moving")]
    public bool RefreshDOTWhenMoving { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Lily at max stacks/about to overcap.")]
    public bool UseLilyWhenFull { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum health threshold party member needs to be to use Benediction")]
    public float BenedictionHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "If a party member's health drops below this percentage, the Regen healing ability will not be used on them")]
    public float RegenHeal { get; set; } = 0.3f;

    [Range(1, 13, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Number of GCDs before you cap on blue lillies that overcap protection will consider 'near full'.")]
    public int LilyOvercapTime { get; set; } = 3;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [Range(0, 10000, ConfigUnitType.None, 100)]
    [RotationConfig(CombatType.PvE, Name = "Casting cost requirement for Thin Air to be used")]
    public float ThinAirNeed { get; set; } = 1000;

    [RotationConfig(CombatType.PvE, Name = "How to manage the last thin air charge")]
    public ThinAirUsageStrategy ThinAirLastChargeUsage { get; set; } = ThinAirUsageStrategy.ReserveLastChargeForRaise;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Dia maintenance over other damage spells")]
    public bool PrioritizeDiaMaintenance { get; set; } = true;

    public enum ThinAirUsageStrategy : byte
    {
        [Description("Use all thin air charges on expensive spells")]
        UseAllCharges,

        [Description("Reserve the last charge for raise")]
        ReserveLastChargeForRaise,

        [Description("Reserve the last charge for manual use")]
        ReserveLastCharge,
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < StonePvE.Info.CastTime + CountDownAhead
            && StonePvE.CanUse(out IAction? act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool useLastThinAirCharge = ThinAirLastChargeUsage == ThinAirUsageStrategy.UseAllCharges || (ThinAirLastChargeUsage == ThinAirUsageStrategy.ReserveLastChargeForRaise && nextGCD == RaisePvE);
        if (((nextGCD is IBaseAction action && action.Info.MPNeed >= ThinAirNeed) || (MergedStatus.HasFlag(AutoStatus.Raise) && Player.CurrentMp > 2400 && IsLastAction() == IsLastGCD())) &&
            ThinAirPvE.CanUse(out act, usedUp: useLastThinAirCharge))
        {
            return true;
        }

        if (Player.WillStatusEndGCD(2, 0, true, StatusID.DivineGrace) && DivineCaressPvE.CanUse(out act))
        {
            return true;
        }

        if (nextGCD.IsTheSameTo(true, AfflatusRapturePvE, MedicaPvE, MedicaIiPvE, CureIiiPvE)
            && (MergedStatus.HasFlag(AutoStatus.HealAreaSpell) || MergedStatus.HasFlag(AutoStatus.HealSingleSpell)))
        {
            if (PlenaryIndulgencePvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if ((TemperancePvE.Cooldown.IsCoolingDown && !TemperancePvE.Cooldown.WillHaveOneCharge(100))
            || (LiturgyOfTheBellPvE.Cooldown.IsCoolingDown && !LiturgyOfTheBellPvE.Cooldown.WillHaveOneCharge(160)))
        {
            return false;
        }

        if (TemperancePvE.CanUse(out act))
        {
            return true;
        }

        if (DivineCaressPvE.CanUse(out act))
        {
            return true;
        }

        if (LiturgyOfTheBellPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if ((DivineBenisonPvE.Cooldown.IsCoolingDown && !DivineBenisonPvE.Cooldown.WillHaveOneCharge(15))
            || (AquaveilPvE.Cooldown.IsCoolingDown && !AquaveilPvE.Cooldown.WillHaveOneCharge(52)))
        {
            return false;
        }

        if (DivineBenisonPvE.CanUse(out act))
        {
            return true;
        }

        if (AquaveilPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AquaveilPvE.CanUse(out act))
        {
            return true;
        }
        return base.HealAreaAbility(nextGCD, out act);
    }

    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (BenedictionPvE.CanUse(out act) &&
            RegenPvE.Target.Target?.GetHealthRatio() < BenedictionHeal)
        {
            return true;
        }

        if (!IsMoving && AsylumPvE.CanUse(out act))
        {
            return true;
        }

        if (DivineBenisonPvE.CanUse(out act))
        {
            return true;
        }

        if (TetragrammatonPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (InCombat)
        {
            if (UsePresenceOfMind && PresenceOfMindPvE.CanUse(out act))
            {
                return true;
            }

            if (AssizePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;
        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (AfflatusRapturePvE.CanUse(out act))
        {
            return true;
        }

        int hasMedica2 = 0;
        foreach (IBattleChara n in PartyMembers)
        {
            if (n.HasStatus(true, StatusID.MedicaIi))
            {
                hasMedica2++;
            }
        }

        int partyCount = 0;
        foreach (IBattleChara _ in PartyMembers)
        {
            partyCount++;
        }

        if (MedicaIiiPvE.CanUse(out act) && !IsLastAction(true, MedicaIiiPvE))
        {
            return true;
        }

        if (MedicaIiPvE.CanUse(out act) && hasMedica2 < partyCount / 2 && !IsLastAction(true, MedicaIiPvE))
        {
            return true;
        }

        if (CureIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (MedicaPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (AfflatusSolacePvE.CanUse(out act))
        {
            return true;
        }

        if (RegenPvE.CanUse(out act) && (RegenPvE.Target.Target?.GetHealthRatio() > RegenHeal))
        {
            return true;
        }

        if (CureIiPvE.CanUse(out act))
        {
            return true;
        }

        if (CurePvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;

        if (HasThinAir && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.RaiseGCD(out act);
        }

        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        // Priority 1: Afflatus Misery (highest damage when available)
        if (AfflatusMiseryPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Priority 2: Glare IV (Sacred Sight stacks - use in buff windows)
        if (GlareIvPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 3: Dia maintenance (keep DoT up at all times)
        if (UseDOTs && PrioritizeDiaMaintenance && DiaPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 4: Lily management when nearly full/overcapping
        bool liliesNearlyFull = Lily == 2 && LilyTime < LilyOvercapTime;
        bool liliesFullNoBlood = Lily == 3;
        if (UseLilyWhenFull && (liliesNearlyFull || liliesFullNoBlood) && AfflatusMiseryPvE.EnoughLevel && BloodLily < 3)
        {
            if (UseLily(out act))
            {
                return true;
            }
        }

        // Priority 5: AoE damage (3+ targets)
        if (HolyIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (HolyPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 6: Glare III (primary single-target filler)
        if (GlareIiiPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 7: Lower-level DoTs if Dia not available
        if (UseDOTs)
        {
            if (AeroIiPvE.CanUse(out act))
            {
                return true;
            }

            if (AeroPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Priority 8: Lower-level Glare/Stone spells as fallback
        if (GlarePvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIvPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIiPvE.CanUse(out act))
        {
            return true;
        }

        if (StonePvE.CanUse(out act))
        {
            return true;
        }

        // Priority 9: Lily downtime usage (when no valid targets)
        if (liliesNearlyFull || liliesFullNoBlood)
        {
            if (UseLily(out act))
            {
                return true;
            }
        }

        // Priority 10: Movement optimization - refresh DOT early when moving
        if (RefreshDOTWhenMoving && IsMoving && UseDOTs)
        {
            if (DiaPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }

            if (AeroIiPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }

            if (AeroPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public override bool CanHealSingleSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }

    public override bool CanHealAreaSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealAreaSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }

    private bool UseLily(out IAction? act)
    {
        if (AfflatusRapturePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return AfflatusSolacePvE.CanUse(out act);
    }
    #endregion

    #region Display Status
    public override void DisplayStatus()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Apollo WHM - Optimized Rotation:");
        ImGui.Text($"Sacred Sight Stacks: {SacredSightStacks}");
        ImGui.Text($"Dia Maintenance Priority: {PrioritizeDiaMaintenance}");
        ImGui.Text($"Use DOTs: {UseDOTs}");
        ImGui.Text($"Use Presence of Mind: {UsePresenceOfMind}");
        ImGui.Text($"Lily Usage: {UseLily(out _)}");
        ImGui.Text($"Lily Stacks: {Lily}/3, Blood: {BloodLily}/3");
        ImGui.Text($"GCD Heal Enabled: {GCDHeal}");
        base.DisplayStatus();
    }
    #endregion
}