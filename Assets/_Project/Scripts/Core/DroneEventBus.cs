using System;
using UnityEngine;

namespace SolarDrone.Core
{
    /// <summary>
    /// Decoupled event bus enabling clean communication between agent, hardware, and UI layers.
    /// </summary>
    public static class DroneEventBus
    {
        public static event Action<DroneState> OnStateChanged;
        public static event Action<string> OnTargetSelected;
        public static event Action<float, float> OnBatteryUpdated; // current, max
        public static event Action OnLowBatteryTriggered;
        public static event Action<string, LogSeverity> OnDecisionLogged;
        public static event Action<string, AnomalyType> OnAnomalyDetected;
        public static event Action<string> OnInspectionCompleted;
        public static event Action OnMissionCompleted;

        public static void PublishStateChanged(DroneState state) => OnStateChanged?.Invoke(state);
        public static void PublishTargetSelected(string panelId) => OnTargetSelected?.Invoke(panelId);
        public static void PublishBatteryUpdated(float current, float max) => OnBatteryUpdated?.Invoke(current, max);
        public static void PublishLowBatteryTriggered() => OnLowBatteryTriggered?.Invoke();
        public static void PublishDecisionLogged(string message, LogSeverity severity = LogSeverity.Info) => OnDecisionLogged?.Invoke(message, severity);
        public static void PublishAnomalyDetected(string panelId, AnomalyType anomaly) => OnAnomalyDetected?.Invoke(panelId, anomaly);
        public static void PublishInspectionCompleted(string panelId) => OnInspectionCompleted?.Invoke(panelId);
        public static void PublishMissionCompleted() => OnMissionCompleted?.Invoke();

        public static void ClearAllSubscribers()
        {
            OnStateChanged = null;
            OnTargetSelected = null;
            OnBatteryUpdated = null;
            OnLowBatteryTriggered = null;
            OnDecisionLogged = null;
            OnAnomalyDetected = null;
            OnInspectionCompleted = null;
            OnMissionCompleted = null;
        }
    }
}
