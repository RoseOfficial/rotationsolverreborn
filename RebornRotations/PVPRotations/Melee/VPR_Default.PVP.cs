namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.3")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/VPR_Default.PvP.cs")]
[Api(6)]
public sealed class VPR_DefaultPvP : ViperRotation
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
        if ((RespectGuard && Player.HasStatus(true, StatusID.Guard)) || Player.HasStatus(true, StatusID.HardenedScales))
        {
            return false;
        }

        if (DoPurify(out action))
        {
            return true;
        }

        //these have to stay in Emergency because action weirdness with Serpent's Tail adjust ID
        if (UncoiledTwinbloodPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledTwinfangPvP.CanUse(out action))
        {
            return true;
        }

        if (RattlingCoilPvP.CanUse(out action))
        {
            if (SnakeScalesPvP.Cooldown.IsCoolingDown && UncoiledFuryPvP.Cooldown.IsCoolingDown)
            {
                return true;
            }
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

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if ((RespectGuard && Player.HasStatus(true, StatusID.Guard)) || Player.HasStatus(true, StatusID.HardenedScales))
        {
            return false;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if ((RespectGuard && Player.HasStatus(true, StatusID.Guard)) || Player.HasStatus(true, StatusID.HardenedScales))
        {
            return false;
        }

        if (FourthLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (ThirdLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (SecondLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (FirstLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (TwinbloodBitePvP.CanUse(out action))
        {
            return true;
        }

        if (TwinfangBitePvP.CanUse(out action))
        {
            return true;
        }

        if (DeathRattlePvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if ((RespectGuard && Player.HasStatus(true, StatusID.Guard)) || Player.HasStatus(true, StatusID.HardenedScales))
        {
            return false;
        }

        return base.MoveForwardAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if ((RespectGuard && Player.HasStatus(true, StatusID.Guard)) || Player.HasStatus(true, StatusID.HardenedScales))
        {
            return false;
        }

        if (FourthGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (ThirdGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (SecondGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (FirstGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (OuroborosPvP.CanUse(out action))
        {
            return true;
        }

        if (SanguineFeastPvP.CanUse(out action))
        {
            return true;
        }

        if (BloodcoilPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledFuryPvP.CanUse(out action))
        {
            return true;
        }

        if (RavenousBitePvP.CanUse(out action))
        {
            return true;
        }

        if (SwiftskinsStingPvP.CanUse(out action))
        {
            return true;
        }

        if (PiercingFangsPvP.CanUse(out action))
        {
            return true;
        }

        if (BarbarousBitePvP.CanUse(out action))
        {
            return true;
        }

        if (HuntersStingPvP.CanUse(out action))
        {
            return true;
        }

        if (SteelFangsPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledFuryPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}