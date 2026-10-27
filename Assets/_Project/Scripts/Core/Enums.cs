namespace SolarDrone.Core
{
    /// <summary>
    /// Lifecycle states for the autonomous inspection drone agent.
    /// </summary>
    public enum DroneState
    {
        IdleAtBase,
        TakingOff,
        NavigatingToPanel,
        PrimaryInspection,
        SecondaryInspection,
        ReturningToBase,
        Landing,
        MissionComplete
    }

    /// <summary>
    /// Anomaly classification types detected during solar panel inspection.
    /// </summary>
    public enum AnomalyType
    {
        None,
        ThermalHotspot,
        PhysicalCrack,
        HeavySoiling
    }

    /// <summary>
    /// Severity level for AI decision events and telemetry logs.
    /// </summary>
    public enum LogSeverity
    {
        Info,
        Warning,
        Alert,
        Success
    }
}
