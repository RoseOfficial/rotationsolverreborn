using Dalamud.Interface.Colors;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace RebornRotations.Healer;

[Rotation("Apollo v3", CombatType.PvE, GameVersion = "7.25")]
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
    [RotationConfig(CombatType.PvE, Name = "Proactive Regen threshold - apply Regen when party member reaches this HP")]
    public float ProactiveRegenThreshold { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "If a party member's health drops below this percentage, the Regen healing ability will not be used on them")]
    public float RegenHeal { get; set; } = 0.3f;

    [Range(1, 13, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Number of GCDs before you cap on blue lillies that overcap protection will consider 'near full'.")]
    public int LilyOvercapTime { get; set; } = 3;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "MP threshold below which to prioritize Thin Air usage")]
    public float ThinAirMPThreshold { get; set; } = 0.6f;

    [RotationConfig(CombatType.PvE, Name = "How to manage the last thin air charge")]
    public ThinAirUsageStrategy ThinAirLastChargeUsage { get; set; } = ThinAirUsageStrategy.ReserveLastChargeForRaise;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Dia maintenance over other damage spells")]
    public bool PrioritizeDiaMaintenance { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable buff window optimization (coordinates abilities with party buffs)")]
    public bool EnableBuffWindowOptimization { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable proactive healing mode (heal preemptively vs reactively)")]
    public bool EnableProactiveHealing { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Proactive healing threshold - start healing when party member reaches this HP")]
    public float ProactiveHealThreshold { get; set; } = 0.7f;

    [RotationConfig(CombatType.PvE, Name = "Preemptively apply Divine Benison to tanks and healers")]
    public bool PreemptiveBenison { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold Presence of Mind for buff windows")]
    public bool HoldPresenceOfMindForBuffs { get; set; } = true;

    [Range(5, 30, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Maximum time to hold Presence of Mind for buff window (seconds)")]
    public float PresenceOfMindHoldTime { get; set; } = 15f;

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

    #region Strategic Thin Air Logic
    /// <summary>
    /// Determines if Thin Air should be used based on current best practices:
    /// Priority 1: Raise (2400 MP) - Always use
    /// Priority 2: Expensive AoE heals (1300+ MP) - Use when MP is below threshold or critical healing
    /// Priority 3: Basic spells (400 MP) - Only use when MP is low and no better options
    /// </summary>
    private bool ShouldUseThinAir(IAction nextGCD, bool useLastCharge)
    {
        if (nextGCD is not IBaseAction action) return false;
        
        float currentMPRatio = (float)Player.CurrentMp / Player.MaxMp;
        int mpCost = (int)action.Info.MPNeed;
        
        // Priority 1: Always use for Raise (highest priority)
        if (nextGCD == RaisePvE)
        {
            return true;
        }
        
        // Priority 2: Expensive AoE healing spells (1300+ MP)
        if (mpCost >= 1300 && (nextGCD == MedicaIiiPvE || nextGCD == CureIiiPvE || nextGCD == MedicaIiPvE))
        {
            // Use if MP is below threshold OR in critical healing situation
            return currentMPRatio < ThinAirMPThreshold || MergedStatus.HasFlag(AutoStatus.HealAreaSpell);
        }
        
        // Priority 3: Medium cost heals (800+ MP) - use when MP is low
        if (mpCost >= 800 && currentMPRatio < ThinAirMPThreshold)
        {
            return nextGCD == CureIiPvE || nextGCD == CurePvE || nextGCD == MedicaPvE;
        }
        
        // Priority 4: Damage spells (400 MP) - only use when MP is critically low and no better options expected
        if (mpCost >= 400 && currentMPRatio < 0.3f)
        {
            // Only use on damage spells if we're at max charges or MP is critically low
            return ThinAirPvE.Cooldown.CurrentCharges >= 2 || currentMPRatio < 0.2f;
        }
        
        return false;
    }
    #endregion

    #region Buff Window Optimization Logic
    /// <summary>
    /// Detects if party raid buffs are active or incoming for optimal burst timing
    /// </summary>
    private bool IsInBuffWindow()
    {
        if (!EnableBuffWindowOptimization) return false;

        // Check for common raid buffs that boost damage
        var commonRaidBuffs = new[]
        {
            StatusID.Divination,        // AST card buffs
            StatusID.ArcaneCircle,      // RPR
            StatusID.BattleLitany,      // DRG
            StatusID.Brotherhood,       // MNK
            StatusID.Devotion,          // SMN
            StatusID.BattleVoice,       // BRD
            StatusID.RadiantFinale,     // BRD
            StatusID.StandardFinish,    // DNC
            StatusID.TechnicalFinish,   // DNC
            StatusID.Embolden,          // RDM
            StatusID.ChainStratagem,    // SCH
        };

        // Check if player has any raid buffs
        foreach (var buffId in commonRaidBuffs)
        {
            if (Player.HasStatus(true, buffId))
            {
                return true;
            }
        }

        // Check party members for buff application (for buffs that affect the whole party)
        foreach (var member in PartyMembers)
        {
            foreach (var buffId in commonRaidBuffs)
            {
                if (member.HasStatus(true, buffId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a buff window is expected soon based on common 2-minute cycles
    /// </summary>
    private bool IsBuffWindowSoon()
    {
        if (!EnableBuffWindowOptimization) return false;

        // Basic 2-minute cycle detection (120s intervals)
        // This is a simplified version - more sophisticated timing could be added
        float combatTime = CombatTime;
        if (combatTime < 10) return true; // Always prepare for opener

        // Check for upcoming 2-minute windows (110-120s, 230-240s, etc.)
        float cyclePosition = combatTime % 120f;
        return cyclePosition >= 110f || cyclePosition <= 10f;
    }

    /// <summary>
    /// Determines if we should hold Presence of Mind for a better buff window
    /// </summary>
    private bool ShouldHoldPresenceOfMind()
    {
        if (!HoldPresenceOfMindForBuffs || !EnableBuffWindowOptimization) return false;
        if (!PresenceOfMindPvE.CanUse(out _)) return false;

        // Don't hold if we're already in a buff window
        if (IsInBuffWindow()) return false;

        // Don't hold if it's been too long since we could have used it
        if (PresenceOfMindPvE.Cooldown.ElapsedAfter(PresenceOfMindHoldTime)) return false;

        // Hold if a buff window is expected soon
        return IsBuffWindowSoon();
    }
    #endregion

    #region Proactive Healing Logic
    /// <summary>
    /// Identifies party members who need proactive healing based on configurable thresholds
    /// </summary>
    private bool ShouldHealProactively(out IBattleChara? target)
    {
        target = null;
        if (!EnableProactiveHealing) return false;

        // Prioritize tanks and healers for proactive healing
        var priorityTargets = PartyMembers
            .Where(m => !m.IsDead && m.GetHealthRatio() < ProactiveHealThreshold)
            .OrderBy(m => m.IsJobCategory(JobRole.Tank) ? 0 : m.IsJobCategory(JobRole.Healer) ? 1 : 2)
            .ThenBy(m => m.GetHealthRatio());

        target = priorityTargets.FirstOrDefault();
        return target != null;
    }

    /// <summary>
    /// Checks if we should apply proactive Regen to maintain party health
    /// </summary>
    private bool ShouldApplyProactiveRegen(out IBattleChara? target)
    {
        target = null;
        if (!EnableProactiveHealing) return false;

        // Find party members without Regen who are below proactive threshold
        var targetsNeedingRegen = PartyMembers
            .Where(m => !m.IsDead && 
                       m.GetHealthRatio() < ProactiveRegenThreshold && 
                       m.GetHealthRatio() > RegenHeal &&
                       !m.HasStatus(true, StatusID.Regen))
            .OrderBy(m => m.IsJobCategory(JobRole.Tank) ? 0 : m.IsJobCategory(JobRole.Healer) ? 1 : 2)
            .ThenBy(m => m.GetHealthRatio());

        target = targetsNeedingRegen.FirstOrDefault();
        return target != null;
    }

    /// <summary>
    /// Determines if we should preemptively shield tanks/healers with Divine Benison
    /// </summary>
    private bool ShouldApplyPreemptiveBenison(out IBattleChara? target)
    {
        target = null;
        if (!PreemptiveBenison || !DivineBenisonPvE.CanUse(out _)) return false;

        // Priority: Tanks without Divine Benison shield, then healers
        var targetsNeedingShield = PartyMembers
            .Where(m => !m.IsDead && 
                       (m.IsJobCategory(JobRole.Tank) || m.IsJobCategory(JobRole.Healer)) &&
                       !m.HasStatus(true, StatusID.DivineBenison))
            .OrderBy(m => m.IsJobCategory(JobRole.Tank) ? 0 : 1)
            .ThenBy(m => m.GetHealthRatio());

        target = targetsNeedingShield.FirstOrDefault();
        return target != null;
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool useLastThinAirCharge = ThinAirLastChargeUsage == ThinAirUsageStrategy.UseAllCharges || (ThinAirLastChargeUsage == ThinAirUsageStrategy.ReserveLastChargeForRaise && nextGCD == RaisePvE);
        
        // Strategic Thin Air usage based on best practices
        if (ShouldUseThinAir(nextGCD, useLastThinAirCharge) && ThinAirPvE.CanUse(out act, usedUp: useLastThinAirCharge))
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
        // Proactive Divine Benison for tanks/healers
        if (ShouldApplyPreemptiveBenison(out IBattleChara? benisonTarget) && 
            DivineBenisonPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

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
            // Buff window optimized Presence of Mind usage
            if (UsePresenceOfMind && PresenceOfMindPvE.CanUse(out act))
            {
                // Use immediately if in buff window or not using optimization
                if (!EnableBuffWindowOptimization || IsInBuffWindow() || !ShouldHoldPresenceOfMind())
                {
                    return true;
                }
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

        // Proactive healing with lily spells when enabled
        if (EnableProactiveHealing && ShouldHealProactively(out IBattleChara? proactiveTarget))
        {
            if (AfflatusSolacePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (AfflatusSolacePvE.CanUse(out act))
        {
            return true;
        }

        // Proactive Regen application
        if (ShouldApplyProactiveRegen(out IBattleChara? regenTarget) && 
            RegenPvE.CanUse(out act, skipStatusProvideCheck: true))
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

        // Priority 2: Glare IV during buff windows (highest priority for burst)
        if (EnableBuffWindowOptimization && IsInBuffWindow() && GlareIvPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 3: Dia maintenance (keep DoT up at all times)
        if (UseDOTs && PrioritizeDiaMaintenance && DiaPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 4: Glare IV (Sacred Sight stacks - use normally when not in buff window)
        if (GlareIvPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 5: Proactive healing with custom thresholds (independent of global system)
        if (EnableProactiveHealing)
        {
            // Check for proactive healing targets using our custom thresholds
            if (ShouldHealProactively(out IBattleChara? proactiveHealTarget))
            {
                // Use lily spells first for proactive healing
                if (Lily > 0 && AfflatusSolacePvE.CanUse(out act))
                {
                    return true;
                }
                
                // Use regular healing spells if needed and available
                if (CureIiPvE.CanUse(out act))
                {
                    return true;
                }
                
                if (CurePvE.CanUse(out act))
                {
                    return true;
                }
            }
            
            // Apply proactive Regen separately
            if (ShouldApplyProactiveRegen(out IBattleChara? regenTarget) && 
                RegenPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }
        
        // Priority 5a: Emergency healing regardless of proactive setting
        foreach (IBattleChara member in PartyMembers)
        {
            if (!member.IsDead && member.GetHealthRatio() < BenedictionHeal)
            {
                // Critical health - use lily healing if available
                if (Lily > 0 && AfflatusSolacePvE.CanUse(out act))
                {
                    return true;
                }
                
                // Use regular heals for critical targets
                if (CureIiPvE.CanUse(out act))
                {
                    return true;
                }
                
                if (CurePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        // Priority 6: Lily management when nearly full/overcapping (unchanged logic)
        bool liliesNearlyFull = Lily == 2 && LilyTime < LilyOvercapTime;
        bool liliesFullNoBlood = Lily == 3;
        if (UseLilyWhenFull && (liliesNearlyFull || liliesFullNoBlood) && AfflatusMiseryPvE.EnoughLevel && BloodLily < 3)
        {
            if (UseLily(out act))
            {
                return true;
            }
        }

        // Priority 7: AoE damage (3+ targets)
        if (HolyIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (HolyPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 8: Glare III (primary single-target filler)
        if (GlareIiiPvE.CanUse(out act))
        {
            return true;
        }

        // Priority 9: Lower-level DoTs if Dia not available
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

        // Priority 10: Lower-level Glare/Stone spells as fallback
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

        // Priority 11: Lily downtime usage (when no valid targets)
        if (liliesNearlyFull || liliesFullNoBlood)
        {
            if (UseLily(out act))
            {
                return true;
            }
        }

        // Priority 12: Movement optimization - refresh DOT early when moving
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
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Apollo WHM v3 - Buff Window Optimized:");
        ImGui.Text($"Sacred Sight Stacks: {SacredSightStacks}");
        ImGui.Text($"Dia Maintenance Priority: {PrioritizeDiaMaintenance}");
        ImGui.Text($"Use DOTs: {UseDOTs}");
        ImGui.Text($"Use Presence of Mind: {UsePresenceOfMind}");
        ImGui.Text($"Lily Usage: {UseLily(out _)}");
        ImGui.Text($"Lily Stacks: {Lily}/3, Blood: {BloodLily}/3");
        ImGui.Text($"GCD Heal Enabled: {GCDHeal}");
        ImGui.Text($"Current MP: {Player.CurrentMp}/{Player.MaxMp} ({(float)Player.CurrentMp / Player.MaxMp:P1})");
        ImGui.Text($"Thin Air Charges: {ThinAirPvE.Cooldown.CurrentCharges}/2");
        ImGui.Text($"Thin Air MP Threshold: {ThinAirMPThreshold:P0}");
        
        if (EnableBuffWindowOptimization)
        {
            ImGui.Text($"Buff Window Active: {IsInBuffWindow()}");
            ImGui.Text($"Buff Window Soon: {IsBuffWindowSoon()}");
            ImGui.Text($"Holding PoM: {ShouldHoldPresenceOfMind()}");
        }
        
        if (EnableProactiveHealing)
        {
            ImGui.Text($"Proactive Healing: ON");
            ImGui.Text($"Proactive Threshold: {ProactiveHealThreshold:P0}");
            ImGui.Text($"Proactive Regen Threshold: {ProactiveRegenThreshold:P0}");
            ImGui.Text($"Preemptive Benison: {PreemptiveBenison}");
            ImGui.Text($"Should Heal Proactively: {ShouldHealProactively(out _)}");
            ImGui.Text($"Should Apply Proactive Regen: {ShouldApplyProactiveRegen(out _)}");
            ImGui.Text($"Should Apply Preemptive Benison: {ShouldApplyPreemptiveBenison(out _)}");
        }
        
        base.DisplayStatus();
    }
    #endregion
}