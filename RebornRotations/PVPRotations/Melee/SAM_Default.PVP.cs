namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.3")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/SAM_Default.PvP.cs")]
[Api(6)]
public sealed class SAM_DefaultPvP : SamuraiRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
    public float BloodBathPvPPercent { get; set; } = 0.75f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
    public float SmitePvPPercent { get; set; } = 0.25f;

    [RotationConfig(CombatType.PvP, Name = "Allow Mineuchi to be used on any target rather than just targets that already have Kuzushi status.")]
    public bool MineuchiAny { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Allow Hissatsu Soten to be used on any target regardless of distance (good luck)")]
    public bool SotenYeet { get; set; } = false;
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

        if (BloodbathPvP.CanUse(out action))
        {
            if (Player.GetHealthRatio() < BloodBathPvPPercent)
            {
                return true;
            }
        }

        if (SwiftPvP.CanUse(out action))
        {
            return true;
        }

        if (SmitePvP.CanUse(out action))
        {
            if (CurrentTarget?.GetHealthRatio() <= SmitePvPPercent)
            {
                return true;
            }
        }

        if (HissatsuSotenPvP.CanUse(out action, usedUp: true))
        {
            if (!HasHostilesInRange && SotenYeet)
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (HissatsuChitenPvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (HissatsuSotenPvP.CanUse(out action, usedUp: true))
        {
            if (nextGCD.IsTheSameTo(false, ActionID.YukikazePvP, ActionID.GekkoPvP, ActionID.KashaPvP))
            {
                return true;
            }
        }

        if (ZanshinPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (MineuchiPvP.CanUse(out action, skipTargetStatusNeedCheck: MineuchiAny))
        {
            return true;
        }

        if (HissatsuChitenPvP.CanUse(out action))
        {
            if (HasHostilesInRange)
            {
                return true;
            }
        }

        if (MeikyoShisuiPvP.CanUse(out action))
        {
            if (HasHostilesInRange)
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.HissatsuSotenPvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (HissatsuSotenPvP.CanUse(out action))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (TendoKaeshiSetsugekkaPvP.CanUse(out action))
        {
            return true;
        }

        if (TendoSetsugekkaPvP.CanUse(out action))
        {
            return true;
        }

        if (KaeshiNamikiriPvP.CanUse(out action))
        {
            return true;
        }

        if (OgiNamikiriPvP.CanUse(out action))
        {
            return true;
        }

        if (KashaPvP.CanUse(out action))
        {
            return true;
        }

        if (GekkoPvP.CanUse(out action))
        {
            return true;
        }

        if (YukikazePvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}