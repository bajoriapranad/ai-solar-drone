using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.Drone
{
    /// <summary>
    /// Simulates onboard battery discharge during flight and triggers safety alerts.
    /// </summary>
    public class DroneBattery : MonoBehaviour
    {
        [Header("Battery Capacity")]
        [SerializeField] private float maxBattery = 100.0f;
        [SerializeField] private float currentBattery = 100.0f;

        [Header("Discharge Settings")]
        [SerializeField] private float dischargeRatePerSecond = 2.5f;
        [SerializeField] private float lowBatteryThreshold = 25.0f;
        [SerializeField] private bool dischargeEnabled = true;

        private bool hasTriggeredLowBattery = false;

        public float MaxBattery => maxBattery;
        public float CurrentBattery => currentBattery;
        public float BatteryPercentage => (currentBattery / maxBattery) * 100f;
        public bool IsLowBattery => currentBattery <= lowBatteryThreshold;
        public float DischargeRatePerSecond => dischargeRatePerSecond;

        private void Start()
        {
            DroneEventBus.PublishBatteryUpdated(currentBattery, maxBattery);
        }

        private void Update()
        {
            if (!dischargeEnabled || currentBattery <= 0f) return;

            currentBattery -= dischargeRatePerSecond * Time.deltaTime;
            currentBattery = Mathf.Max(0f, currentBattery);

            DroneEventBus.PublishBatteryUpdated(currentBattery, maxBattery);

            if (!hasTriggeredLowBattery && currentBattery <= lowBatteryThreshold)
            {
                hasTriggeredLowBattery = true;
                DroneEventBus.PublishLowBatteryTriggered();
                DroneEventBus.PublishDecisionLogged(
                    $"[BATTERY CRITICAL] Charge at {BatteryPercentage:F1}% (below {lowBatteryThreshold}%). Requesting Return-To-Base override!",
                    LogSeverity.Alert
                );
            }
        }

        public void SetDischargeEnabled(bool enabled)
        {
            dischargeEnabled = enabled;
        }

        public void Recharge()
        {
            currentBattery = maxBattery;
            hasTriggeredLowBattery = false;
            DroneEventBus.PublishBatteryUpdated(currentBattery, maxBattery);
            DroneEventBus.PublishDecisionLogged("[BATTERY] Full charge restored.", LogSeverity.Success);
        }

        public void SetBatteryLevel(float level)
        {
            currentBattery = Mathf.Clamp(level, 0f, maxBattery);
            if (currentBattery > lowBatteryThreshold)
            {
                hasTriggeredLowBattery = false;
            }
            DroneEventBus.PublishBatteryUpdated(currentBattery, maxBattery);
        }
    }
}
