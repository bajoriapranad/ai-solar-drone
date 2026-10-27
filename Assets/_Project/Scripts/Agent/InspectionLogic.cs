using System;
using System.Collections;
using UnityEngine;
using SolarDrone.Core;
using SolarDrone.Environment;

namespace SolarDrone.Agent
{
    /// <summary>
    /// Autonomous inspection analysis engine. Evaluates optical/thermal sensor feed from panels
    /// and determines anomaly classification and secondary scan requirements.
    /// </summary>
    public class InspectionLogic : MonoBehaviour
    {
        [Header("Scan Timing")]
        [SerializeField] private float primaryScanDuration = 2.0f;
        [SerializeField] private float secondaryScanDuration = 2.5f;

        /// <summary>
        /// Executes primary survey scan. Invokes callback with (hasAnomaly, anomalyType).
        /// </summary>
        public IEnumerator PerformPrimaryScan(SolarPanel targetPanel, Action<bool, AnomalyType> onScanComplete)
        {
            DroneEventBus.PublishDecisionLogged(
                $"[SCANNER] Primary optical & infrared scan initiated on {targetPanel.PanelId}...",
                LogSeverity.Info
            );

            yield return new WaitForSeconds(primaryScanDuration);

            bool hasAnomaly = targetPanel.HasSimulatedAnomaly;
            AnomalyType detectedType = targetPanel.Anomaly;

            if (hasAnomaly)
            {
                DroneEventBus.PublishDecisionLogged(
                    $"[ANOMALY CONFIRMED] Defect flagged on {targetPanel.PanelId}: {detectedType}! Commencing secondary orbital inspection.",
                    LogSeverity.Alert
                );
                DroneEventBus.PublishAnomalyDetected(targetPanel.PanelId, detectedType);
            }
            else
            {
                DroneEventBus.PublishDecisionLogged(
                    $"[SCANNER] {targetPanel.PanelId} certified nominal. No thermal/physical anomalies found.",
                    LogSeverity.Success
                );
            }

            onScanComplete?.Invoke(hasAnomaly, detectedType);
        }

        /// <summary>
        /// Executes close-up secondary verification scan.
        /// </summary>
        public IEnumerator PerformSecondaryScan(SolarPanel targetPanel, Action onScanComplete)
        {
            DroneEventBus.PublishDecisionLogged(
                $"[SCANNER] Close-range diagnostic capture on {targetPanel.PanelId} ({targetPanel.Anomaly})...",
                LogSeverity.Warning
            );

            yield return new WaitForSeconds(secondaryScanDuration);

            DroneEventBus.PublishDecisionLogged(
                $"[SCANNER] Secondary diagnostic complete for {targetPanel.PanelId}. High-res telemetry logged.",
                LogSeverity.Success
            );

            onScanComplete?.Invoke();
        }
    }
}
