namespace DefaultRotations.Ranged;

[Rotation("LTS's PVP", CombatType.PvP, GameVersion = "6.58", Description = "Beta Rotation")]
[SourceCode(Path = "main/DefaultRotations/PVPRotations/Ranged/MCH_Default.PvP.cs")]
[Api(1)]
public sealed class MCH_DefaultPvP : MachinistRotation
{
    [RotationConfig(CombatType.PvP, Name = "Sprint")]
    public bool UseSprintPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Recuperate")]
    public bool UseRecuperatePvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "RecuperateHP%%?")]
    public int RCValue { get; set; } = 75;

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on Stun")]
    public bool Use1343PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on DeepFreeze")]
    public bool Use3219PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on HalfAsleep")]
    public bool Use3022PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on Sleep")]
    public bool Use1348PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on Bind")]
    public bool Use1345PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on Heavy")]
    public bool Use1344PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify on Silence")]
    public bool Use1347PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool GuardCancel { get; set; } = false;

    private bool TryPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP) return false;

        var purifyStatuses = new Dictionary<int, bool>
        {
            { 1343, Use1343PvP },
            { 3219, Use3219PvP },
            { 3022, Use3022PvP },
            { 1348, Use1348PvP },
            { 1345, Use1345PvP },
            { 1344, Use1344PvP },
            { 1347, Use1347PvP }
        };

        foreach (var status in purifyStatuses)
        {
            if (status.Value && Player.HasStatus(true, (StatusID)status.Key))
            {
                return PurifyPvP.CanUse(out action, skipClippingCheck: true);
            }
        }

        return false;
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;
        if (TryPurify(out act)) return true;
        if (UseRecuperatePvP && Player.CurrentHp / Player.MaxHp * 100 < RCValue && RecuperatePvP.CanUse(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;

        // Use WildfirePvP if Overheated
        if (Player.HasStatus(true, StatusID.Overheated_3149) && WildfirePvP.CanUse(out act, skipAoeCheck: true, skipComboCheck: true, skipClippingCheck: true)) return true;

        // Check if BioblasterPvP, AirAnchorPvP, or ChainSawPvP can be used
        if (InCombat && !Player.HasStatus(true, StatusID.Analysis) &&
            (BioblasterPvP.CanUse(out act) && HostileTarget.DistanceToPlayer() <= 12 || AirAnchorPvP.CanUse(out act) || ChainSawPvP.CanUse(out act)) &&
            AnalysisPvP.CanUse(out act, usedUp: true)) return true;

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        // Early exits for Guard status or Sprint usage
        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;
        if (!Player.HasStatus(true, StatusID.Guard) && UseSprintPvP && !Player.HasStatus(true, StatusID.Sprint) && !InCombat && SprintPvP.CanUse(out act)) return true;

        if (!Player.HasStatus(true, StatusID.Overheated_3149) && BlastChargePvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
        // Specific action sequences based on Overheated status or other specific conditions
        if (Player.HasStatus(true, StatusID.Overheated_3149))
        {
            if (HeatBlastPvP.CanUse(out act)) return true;
        }
        else
        {
            if (HostileTarget != null && !HostileTarget.HasStatus(true, StatusID.Guard))
            {
                if (HostileTarget.DistanceToPlayer() <= 12 && ScattergunPvP.CanUse(out act, skipAoeCheck: true)) return true;
            }

            // Priority to Drill, Bioblaster, AirAnchor if Analysis is active
            if (Player.HasStatus(true, StatusID.Analysis))
            {
                if (DrillPvP.CanUse(out act, skipAoeCheck: true) || (BioblasterPvP.CanUse(out act) && HostileTarget.DistanceToPlayer() <= 12) || AirAnchorPvP.CanUse(out act, skipAoeCheck: true)) return true;
            }
        }

        return base.GeneralGCD(out act);
    }
}
