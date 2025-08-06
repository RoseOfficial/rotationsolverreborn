namespace RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.3")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/SCH_Default.PVP.cs")]
[Api(6)]
public class SCH_DefaultPVP : ScholarRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
    #endregion

    #region Standard PVP Utilities
    private bool DoPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP)
        {
            return false;
        }

        List<int> purifiableStatusesIDs = new()
        {
            // Stun, DeepFreeze, HalfAsleep, Sleep, Bind, Heavy, Silence
            1343, 3219, 3022, 1348, 1345, 1344, 1347
        };

        return purifiableStatusesIDs.Any(id => Player.HasStatus(false, (StatusID)id)) && PurifyPvP.CanUse(out action);
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (DoPurify(out action))
        {
            return true;
        }

        if (ChainStratagemPvP.CanUse(out action) && Target.HasStatus(false, StatusID.Guard))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (DiabrosisPvP.CanUse(out action))
        {
            return true;
        }

        if (DeploymentTacticsPvP.CanUse(out action, usedUp: true) && Target.HasStatus(true, StatusID.Biolysis_3089))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (ExpedientPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out action);
    }

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (SummonSeraphPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool DefenseSingleGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (StoneskinIiPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.DefenseSingleGCD(out action);
    }

    protected override bool HealSingleGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (HaelanPvP.CanUse(out action))
        {
            return true;
        }

        if (AdloquiumPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.HealSingleGCD(out action);
    }

    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (BiolysisPvP.CanUse(out action) && Player.HasStatus(true, StatusID.Recitation_3094))
        {
            return true;
        }

        if (AccessionPvP.CanUse(out action))
        {
            return true;
        }

        if (SeraphicHaloPvP.CanUse(out action))
        {
            return true;
        }

        if (BroilIvPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}