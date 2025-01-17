﻿namespace DefaultRotations.Ranged;

[Rotation("Beta Rotations", CombatType.PvE, GameVersion = "6.58", Description = "Additonal contributions to this rotation thanks to Toshi!")]
[SourceCode(Path = "main/DefaultRotations/Ranged/MCH_Beta.cs")]
[Api(1)]
public sealed class MCH_Beta : MachinistRotation
{
    [RotationConfig(CombatType.PvE, Name = "Uses Rook Autoturret/Automaton Queen immediately whenever you get 50 battery")]
    public bool UseQueenWhenever { get; set; } = true;

    #region Countdown logic
    // Defines logic for actions to take during the countdown before combat starts.
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < 5)
        {
            if (ReassemblePvE.CanUse(out var act)) return act;
        }
        if (remainTime < 2)
        {
            if (UseBurstMedicine(out var act)) return act;
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Emergency Logic
    // Determines emergency actions to take based on the next planned GCD action.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // Reassemble Logic
        // Check next GCD action and conditions for Reassemble.
        bool isReassembleUsable =
            //Reassemble current # of charges and double proc protection
            ReassemblePvE.Cooldown.CurrentCharges > 0 && !Player.HasStatus(true, StatusID.Reassembled) &&
            //Chainsaw Level Check and NextGCD Check
            ((ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, ChainSawPvE)) ||
            //AirAnchor Logic
            (AirAnchorPvE.EnoughLevel && nextGCD.IsTheSameTo(true, AirAnchorPvE)) ||
            //Drill Logic
            (DrillPvE.EnoughLevel && !ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
            //Cleanshot Logic
            (!DrillPvE.EnoughLevel && CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE)) ||
            //HotShot Logic
            (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(true, HotShotPvE)));

        // Keeps Ricochet and Gauss cannon Even
        bool isRicochetMore = (GaussRoundPvE.Cooldown.CurrentCharges <= RicochetPvE.Cooldown.CurrentCharges);
        bool isGaussMore = (GaussRoundPvE.Cooldown.CurrentCharges > RicochetPvE.Cooldown.CurrentCharges);

        // Attempt to use Reassemble if it's ready
        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, onLastAbility: true, skipClippingCheck: true, skipComboCheck: true, usedUp: true)) return true;
        }
        // Use Ricochet
        if (isRicochetMore && ((!IsLastAction(true, GaussRoundPvE) && !IsLastAction(true, RicochetPvE) && Player.HasStatus(true, StatusID.Overheated)) || !Player.HasStatus(true, StatusID.Overheated)))

        {
            return RicochetPvE.CanUse(out act, skipAoeCheck: true, usedUp: true);
        }
        // Use Gause
        if (isGaussMore && ((!IsLastAction(true, GaussRoundPvE) && !IsLastAction(true, RicochetPvE) && Player.HasStatus(true, StatusID.Overheated)) || !Player.HasStatus(true, StatusID.Overheated)))
        {
            return GaussRoundPvE.CanUse(out act, usedUp: true);
        }
        return base.EmergencyAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    // Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        // Define conditions under which the Rook Autoturret/Queen can be used.
        bool OpenerQueen = !CombatElapsedLess(20f) && CombatElapsedLess(25f);
        bool CombatTimeQueen = CombatElapsedLess(60f) && !CombatElapsedLess(45f);
        bool WildfireCooldownQueen = WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.ElapsedAfter(105f) && Battery == 100 &&
                    (nextGCD.IsTheSameTo(true, AirAnchorPvE) || nextGCD.IsTheSameTo(true, CleanShotPvE)) || nextGCD.IsTheSameTo(true, HeatedCleanShotPvE) || nextGCD.IsTheSameTo(true, ChainSawPvE);
        bool BatteryCheckQueen = Battery >= 90 && !WildfirePvE.Cooldown.ElapsedAfter(70f);
        bool LastGCDCheckQueen = Battery >= 80 && !WildfirePvE.Cooldown.ElapsedAfter(77.5f) && IsLastGCD(true, AirAnchorPvE);

        // If Wildfire is active, use Hypercharge.....Period
        if (Player.HasStatus(true, StatusID.Wildfire_1946))
        {
            return HyperchargePvE.CanUse(out act, skipClippingCheck: true);
        }
        // Burst
        if (IsBurst)
        {
            if (UseBurstMedicine(out act)) return true;

            {
                if ((IsLastAbility(false, HyperchargePvE) || Heat >= 50) && !CombatElapsedLess(10) && CanUseHyperchargePvE(out _)
                && WildfirePvE.CanUse(out act, onLastAbility: true, skipComboCheck: true)) return true;
            }
        }
        // Use Hypercharge if at least 12 seconds of combat and (if wildfire will not be up in 30 seconds or if you hit 100 heat)
        if (!CombatElapsedLess(12) && !Player.HasStatus(true, StatusID.Reassembled) && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || (Heat == 100)))
        {
            if (CanUseHyperchargePvE(out act)) return true;
        }
        // Rook Autoturret/Queen Logic toggle on
        if (UseQueenWhenever && (OpenerQueen || CombatTimeQueen || WildfireCooldownQueen || BatteryCheckQueen || LastGCDCheckQueen))
        {
            return RookAutoturretPvE.CanUse(out act, skipComboCheck: true);
        }
        // Rook Autoturret/Queen Logic toggle off
        if (!UseQueenWhenever)
        {
            return RookAutoturretPvE.CanUse(out act, skipComboCheck: true);
        }
        // Use Barrel Stabilizer on CD if won't cap
        if (BarrelStabilizerPvE.CanUse(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // Defines the general logic for determining which global cooldown (GCD) action to take.
    protected override bool GeneralGCD(out IAction? act)
    {
        // Checks and executes AutoCrossbow or HeatBlast if conditions are met (overheated state).
        if (AutoCrossbowPvE.CanUse(out act)) return true;
        if (HeatBlastPvE.CanUse(out act)) return true;

        // Executes Bioblaster, and then checks for AirAnchor or HotShot, and Drill based on availability and conditions.
        if (BioblasterPvE.CanUse(out act)) return true;
        // Check if SpreadShot cannot be used
        if (!SpreadShotPvE.CanUse(out _))
        {
            // Check if AirAnchor can be used
            if (AirAnchorPvE.CanUse(out act)) return true;

            // If not at the required level for AirAnchor and HotShot can be used
            if (!AirAnchorPvE.EnoughLevel && HotShotPvE.CanUse(out act)) return true;

            // Check if Drill can be used
            if (DrillPvE.CanUse(out act)) return true;
        }

        // Special condition for using ChainSaw outside of AoE checks if no action is chosen within 4 GCDs.
        if (!CombatElapsedLessGCD(4) && ChainSawPvE.CanUse(out act, skipAoeCheck: true)) return true;

        // AoE actions: ChainSaw and SpreadShot based on their usability.
        if (SpreadShotPvE.CanUse(out _))
        {
            if (ChainSawPvE.CanUse(out act)) return true;
        }
        if (SpreadShotPvE.CanUse(out act)) return true;

        // Single target actions: CleanShot, SlugShot, and SplitShot based on their usability.
        if (CleanShotPvE.CanUse(out act)) return true;
        if (SlugShotPvE.CanUse(out act)) return true;
        if (SplitShotPvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Extra private helper methods for determining the usability of specific abilities under certain conditions.
    // These methods simplify the main logic by encapsulating specific checks related to abilities' cooldowns and prerequisites.
    // Logic for Hypercharge
    private bool CanUseHyperchargePvE(out IAction? act)
    {
        float REST_TIME = 6f;
        if
                     //Cannot AOE
                     ((!SpreadShotPvE.CanUse(out _))
                     &&
                     // AirAnchor Enough Level % AirAnchor 
                     ((AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(REST_TIME))
                     ||
                     // HotShot Charge Detection
                     (!AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(REST_TIME))
                     ||
                     // Drill Charge Detection
                     (DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveOneCharge(REST_TIME))
                     ||
                     // Chainsaw Charge Detection
                     (ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(REST_TIME))))
        {
            act = null;
            return false;
        }
        else
        {
            // Use Hypercharge
            return HyperchargePvE.CanUse(out act);
        }
    }
    #endregion
}
